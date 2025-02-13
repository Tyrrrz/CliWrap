using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Exceptions;
using CliWrap.Utils;
using CliWrap.Utils.Extensions;

namespace CliWrap;

public partial class Command
{
    // System.Diagnostics.Process already resolves the full path by itself, but it naively assumes that the file
    // is an executable if the extension is omitted. On Windows, BAT and CMD files may also be valid targets.
    // In practice, it means that Process.Start("foo") will work if it's an EXE file, but will fail if it's a
    // BAT or CMD file, even if it's on the PATH. If the extension is specified, it will work in both cases.
    private string GetOptimallyQualifiedTargetFilePath()
    {
        // Currently, we only need this workaround for script files on Windows, so short-circuit
        // if we are on a different platform.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return TargetFilePath;
        }

        // Don't do anything for fully qualified paths or paths that already have an extension specified.
        // System.Diagnostics.Process knows how to handle those without our help.
        // Note that IsPathRooted(...) doesn't check if the path is absolute, as it also returns true for
        // strings like 'c:foo.txt' (which is relative to the current directory on drive C), but it's good
        // enough for our purposes and the alternative is only available on .NET Standard 2.1+.
        if (
            Path.IsPathRooted(TargetFilePath)
            || !string.IsNullOrWhiteSpace(Path.GetExtension(TargetFilePath))
        )
        {
            return TargetFilePath;
        }

        static IEnumerable<string> GetProbeDirectoryPaths()
        {
            // Implementation reference:
            // https://github.com/dotnet/runtime/blob/9a50493f9f1125fda5e2212b9d6718bc7cdbc5c0/src/libraries/System.Diagnostics.Process/src/System/Diagnostics/Process.Unix.cs#L686-L728
            // MIT License, .NET Foundation

            // Executable directory
            if (!string.IsNullOrWhiteSpace(EnvironmentEx.ProcessPath))
            {
                var processDirPath = Path.GetDirectoryName(EnvironmentEx.ProcessPath);
                if (!string.IsNullOrWhiteSpace(processDirPath))
                    yield return processDirPath;
            }

            // Working directory
            yield return Directory.GetCurrentDirectory();

            // Directories on the PATH
            if (Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) is { } paths)
            {
                foreach (var path in paths)
                    yield return path;
            }
        }

        return (
                from probeDirPath in GetProbeDirectoryPaths()
                where Directory.Exists(probeDirPath)
                select Path.Combine(probeDirPath, TargetFilePath) into baseFilePath
                from extension in new[] { "exe", "cmd", "bat" }
                select Path.ChangeExtension(baseFilePath, extension)
            ).FirstOrDefault(File.Exists) ?? TargetFilePath;
    }

    private ProcessStartInfo CreateStartInfo()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = GetOptimallyQualifiedTargetFilePath(),
            Arguments = Arguments,
            WorkingDirectory = WorkingDirPath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            // This option only works on Windows and is required there to prevent the
            // child processes from attaching to the parent console window, if one exists.
            // We need this in order to be able to send signals to one specific child process,
            // without affecting any others that may also be running in parallel.
            // https://github.com/Tyrrrz/CliWrap/issues/47
            CreateNoWindow = true,
        };

        // Set credentials
        try
        {
            // Disable CA1416 because we're handling an exception that is thrown by the property setters
#pragma warning disable CA1416
            if (Credentials.Domain is not null)
                startInfo.Domain = Credentials.Domain;

            if (Credentials.UserName is not null)
                startInfo.UserName = Credentials.UserName;

            if (Credentials.Password is not null)
                startInfo.Password = Credentials.Password.ToSecureString();

            if (Credentials.LoadUserProfile)
                startInfo.LoadUserProfile = Credentials.LoadUserProfile;
#pragma warning restore CA1416
        }
        catch (NotSupportedException ex)
        {
            throw new NotSupportedException(
                "Cannot start a process using the provided credentials. "
                    + "Setting custom domain, username, password, and/or loading the user profile is not supported on this platform.",
                ex
            );
        }

        // Set environment variables
        foreach (var (key, value) in EnvironmentVariables)
        {
            if (value is not null)
            {
                startInfo.Environment[key] = value;
            }
            else
            {
                // Null value means we should remove the variable
                // https://github.com/Tyrrrz/CliWrap/issues/109
                // https://github.com/dotnet/runtime/issues/34446
                startInfo.Environment.Remove(key);
            }
        }

        return startInfo;
    }

    private async Task PipeStandardInputAsync(
        ProcessEx process,
        CancellationToken cancellationToken = default
    )
    {
        await using (process.StandardInput.ToAsyncDisposable())
        {
            try
            {
                await StandardInputPipe
                    .CopyToAsync(process.StandardInput, cancellationToken)
                    // Some streams do not support cancellation, so we add a fallback that
                    // drops the task and returns early.
                    // This is important with stdin because the process might finish before
                    // the pipe has been fully exhausted, and we don't want to wait for it.
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            // Expect IOException: "The pipe has been ended" (Windows) or "Broken pipe" (Unix).
            // This may happen if the process terminated before the pipe has been exhausted.
            // It's not an exceptional situation because the process may not need the entire
            // stdin to complete successfully.
            // Don't catch derived exceptions, such as FileNotFoundException, to avoid false positives.
            // We also can't rely on process.HasExited here because of potential race conditions.
            catch (IOException ex) when (ex.GetType() == typeof(IOException)) { }
        }
    }

    private async Task PipeStandardOutputAsync(
        ProcessEx process,
        CancellationToken cancellationToken = default
    )
    {
        await using (process.StandardOutput.ToAsyncDisposable())
        {
            await StandardOutputPipe
                .CopyFromAsync(process.StandardOutput, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task PipeStandardErrorAsync(
        ProcessEx process,
        CancellationToken cancellationToken = default
    )
    {
        await using (process.StandardError.ToAsyncDisposable())
        {
            await StandardErrorPipe
                .CopyFromAsync(process.StandardError, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<CommandResult> ExecuteAsync(
        ProcessEx process,
        CancellationToken forcefulCancellationToken = default,
        CancellationToken gracefulCancellationToken = default
    )
    {
        using var _ = process;

        // Additional cancellation to ensure we don't wait forever for the process to terminate
        // after forceful cancellation.
        // Ideally, we don't want ExecuteAsync() to return or throw before the process actually
        // exits, but it's theoretically possible that an attempt to kill the process may fail,
        // so we need a fallback.
        using var waitTimeoutCts = new CancellationTokenSource();
        await using var _1 = forcefulCancellationToken
            .Register(
                () =>
                    // ReSharper disable once AccessToDisposedClosure
                    waitTimeoutCts.CancelAfter(TimeSpan.FromSeconds(3))
            )
            .ToAsyncDisposable();

        // Additional cancellation for the stdin pipe in case the process exits without fully exhausting it
        using var stdInCts = CancellationTokenSource.CreateLinkedTokenSource(
            forcefulCancellationToken
        );

        // Bind user-provided cancellation tokens to the process
        await using var _2 = forcefulCancellationToken.Register(process.Kill).ToAsyncDisposable();
        await using var _3 = gracefulCancellationToken
            .Register(process.Interrupt)
            .ToAsyncDisposable();

        // Start piping streams in the background
        var pipingTask = Task.WhenAll(
            PipeStandardInputAsync(process, stdInCts.Token),
            PipeStandardOutputAsync(process, forcefulCancellationToken),
            PipeStandardErrorAsync(process, forcefulCancellationToken)
        );

        try
        {
            // Wait until the process exits normally or gets killed.
            // The timeout is started after the execution is forcefully canceled and ensures
            // that we don't wait forever in case the attempt to kill the process failed.
            await process.WaitUntilExitAsync(waitTimeoutCts.Token).ConfigureAwait(false);

            // Send the cancellation signal to the stdin pipe since the process has exited
            // and won't need it anymore.
            // If the pipe has already been exhausted (most likely), this won't do anything.
            // If the pipe is still trying to transfer data, this will cause it to abort.
            await stdInCts.CancelAsync();

            // Wait until piping is done and propagate exceptions
            await pipingTask.ConfigureAwait(false);
        }
        // Swallow exceptions caused by internal and user-provided cancellations,
        // because we have a separate mechanism for handling them below.
        catch (OperationCanceledException ex)
            when (ex.CancellationToken == forcefulCancellationToken
                || ex.CancellationToken == gracefulCancellationToken
                || ex.CancellationToken == waitTimeoutCts.Token
                || ex.CancellationToken == stdInCts.Token
            ) { }

        // Throw if forceful cancellation was requested.
        // This needs to be checked first because it effectively overrides graceful cancellation
        // by outright killing the process, even if graceful cancellation was requested earlier.
        forcefulCancellationToken.ThrowIfCancellationRequested(
            "Command execution canceled. "
                + $"Underlying process ({process.Name}#{process.Id}) was forcefully terminated."
        );

        // Throw if graceful cancellation was requested
        gracefulCancellationToken.ThrowIfCancellationRequested(
            "Command execution canceled. "
                + $"Underlying process ({process.Name}#{process.Id}) was gracefully terminated."
        );

        // Validate the exit code if required
        if (process.ExitCode != 0 && Validation.IsZeroExitCodeValidationEnabled())
        {
            throw new CommandExecutionException(
                this,
                process.ExitCode,
                $"""
                Command execution failed because the underlying process ({process.Name}#{process.Id}) returned a non-zero exit code ({process.ExitCode}).

                Command:
                {TargetFilePath} {Arguments}

                You can suppress this validation by calling `{nameof(WithValidation)}({nameof(
                    CommandResultValidation
                )}.{nameof(CommandResultValidation.None)})` on the command.
                """
            );
        }

        return new CommandResult(process.ExitCode, process.StartTime, process.ExitTime);
    }

    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    /// <remarks>
    /// This method can be awaited.
    /// </remarks>
    // TODO: (breaking change) use optional parameters and remove the other overload
    public CommandTask<CommandResult> ExecuteAsync(
        CancellationToken forcefulCancellationToken,
        CancellationToken gracefulCancellationToken
    )
    {
        var process = new ProcessEx(CreateStartInfo());

        // This method may fail, and we want to propagate the exceptions immediately instead
        // of wrapping them in a task, so it needs to be executed in a synchronous context.
        // https://github.com/Tyrrrz/CliWrap/issues/139
        process.Start(p =>
        {
            try
            {
                // Disable CA1416 because we're handling an exception that is thrown by the property setters
#pragma warning disable CA1416
                p.PriorityClass = ResourcePolicy.Priority;

                if (ResourcePolicy.Affinity is not null)
                    p.ProcessorAffinity = ResourcePolicy.Affinity.Value;

                if (ResourcePolicy.MinWorkingSet is not null)
                    p.MinWorkingSet = ResourcePolicy.MinWorkingSet.Value;

                if (ResourcePolicy.MaxWorkingSet is not null)
                    p.MaxWorkingSet = ResourcePolicy.MaxWorkingSet.Value;
#pragma warning restore CA1416
            }
            catch (NotSupportedException ex)
            {
                throw new NotSupportedException(
                    "Cannot set resource policy for the process. "
                        + "Setting custom priority, affinity, and/or working set limits is not supported on this platform.",
                    ex
                );
            }
            catch (InvalidOperationException)
            {
                // This exception could indicate that the process has exited before we had a chance to set the policy.
                // This is not an exceptional situation, so we don't need to do anything here.
            }
        });

        // Extract the process ID before calling ExecuteAsync(), because the process may
        // already be disposed by then.
        var processId = process.Id;

        return new CommandTask<CommandResult>(
            ExecuteAsync(process, forcefulCancellationToken, gracefulCancellationToken),
            processId
        );
    }

    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    /// <remarks>
    /// This method can be awaited.
    /// </remarks>
    public CommandTask<CommandResult> ExecuteAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(cancellationToken, CancellationToken.None);
}
