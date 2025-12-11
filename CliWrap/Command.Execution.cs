using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        if (!OperatingSystem.IsWindows())
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
            if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
            {
                var processDirPath = Path.GetDirectoryName(Environment.ProcessPath);
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
                    // The input pipe may never respond to cancellation, so we add a fallback
                    // that drops the task and returns early when cancellation is requested.
                    // This prevents hanging when the process exits before consuming all stdin data.
                    // https://github.com/Tyrrrz/CliWrap/issues/74
                    // Update: after some retrospection, I think it was a bad design decision to
                    // take responsibility for adding a timeout on a user-provided pipe source.
                    // It should be the user's responsibility to ensure that their pipe source
                    // respects cancellation. Otherwise, we may as well add such fallbacks everywhere.
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

    private async Task PipeStreamAsync(
        Stream source,
        PipeTarget target,
        CancellationToken cancellationToken = default
    )
    {
        await using (source.ToAsyncDisposable())
        {
            await target.CopyFromAsync(source, cancellationToken).ConfigureAwait(false);
        }
    }

    // PTY-specific piping methods

    private async Task PipePtyInputAsync(
        PtyProcessEx process,
        CancellationToken cancellationToken = default
    )
    {
        // IMPORTANT: Do NOT dispose the stdin stream here!
        // With PTY, closing the input pipe causes ConPTY to send Ctrl+C to the process.
        // The stream will be cleaned up when the PTY is disposed after the process exits.
        try
        {
            await StandardInputPipe
                .CopyToAsync(process.StandardInput, cancellationToken)
                .ConfigureAwait(false);

            // Flush to ensure data is sent to the PTY immediately
            await process.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (IOException) { }
        catch (OperationCanceledException) { }
    }

    private async Task PipePtyOutputAsync(
        PtyProcessEx process,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Read in a loop to handle the synchronous PTY output stream
            var buffer = new byte[4096];
            int bytesRead;
            while (
                (
                    bytesRead = await Task.Run(
                            () => process.StandardOutput.Read(buffer, 0, buffer.Length),
                            cancellationToken
                        )
                        .ConfigureAwait(false)
                ) > 0
            )
            {
                await StandardOutputPipe
                    .CopyFromAsync(new MemoryStream(buffer, 0, bytesRead), cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
        catch (OperationCanceledException) { }
    }

    private void ThrowIfCanceled(
        IProcessEx process,
        CancellationToken forcefulCancellationToken,
        CancellationToken gracefulCancellationToken,
        bool isPty
    )
    {
        var processType = isPty ? "PTY process" : "process";

        if (forcefulCancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(
                $"Command execution canceled. Underlying {processType} ({process.Name}#{process.Id}) was forcefully terminated.",
                forcefulCancellationToken
            );
        }

        if (gracefulCancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(
                $"Command execution canceled. Underlying {processType} ({process.Name}#{process.Id}) was gracefully terminated.",
                gracefulCancellationToken
            );
        }
    }

    private void ValidateExitCode(IProcessEx process, bool isPty)
    {
        if (process.ExitCode != 0 && Validation.HasFlag(CommandResultValidation.ZeroExitCode))
        {
            var processType = isPty ? "PTY process" : "process";
            throw new CommandExecutionException(
                this,
                process.ExitCode,
                $"""
                Command execution failed because the underlying {processType} ({process.Name}#{process.Id}) returned a non-zero exit code ({process.ExitCode}).

                Command:
                {TargetFilePath} {Arguments}

                You can suppress this validation by calling `{nameof(WithValidation)}({nameof(
                    CommandResultValidation
                )}.{nameof(CommandResultValidation.None)})` on the command.
                """
            );
        }
    }

    private TimeoutException CreateTimeoutException(IProcessEx process, bool isPty, Exception inner)
    {
        var processType = isPty ? "PTY process" : "process";
        return new TimeoutException(
            $"Failed to terminate the underlying {processType} ({process.Name}#{process.Id}) within the allotted timeout.",
            inner
        );
    }

    private string? CreateEnvironmentBlock()
    {
        if (EnvironmentVariables.Count == 0)
            return null;

        // Get current environment and merge with custom variables
        var env = Environment
            .GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(e => (string)e.Key, e => (string?)e.Value);

        foreach (var (key, value) in EnvironmentVariables)
        {
            if (value is not null)
                env[key] = value;
            else
                env.Remove(key);
        }

        // Build null-terminated environment block
        return string.Join("\0", env.Select(kv => $"{kv.Key}={kv.Value}")) + "\0";
    }

    private async Task<CommandResult> ExecuteAsync(
        PtyProcessEx process,
        CancellationToken forcefulCancellationToken = default,
        CancellationToken gracefulCancellationToken = default
    )
    {
        using var _ = process;

        using var waitTimeoutCts = new CancellationTokenSource();
        await using var _1 = forcefulCancellationToken
            .Register(() => waitTimeoutCts.CancelAfter(TimeSpan.FromSeconds(3)))
            .ToAsyncDisposable();

        // CTS for stdin - canceled when process exits or forceful cancellation
        using var stdInCts = CancellationTokenSource.CreateLinkedTokenSource(
            forcefulCancellationToken
        );

        // CTS for stdout - the PTY output stream doesn't get EOF until the console is closed,
        // so we need to cancel output piping after the process exits
        using var stdOutCts = CancellationTokenSource.CreateLinkedTokenSource(
            forcefulCancellationToken
        );

        await using var _2 = forcefulCancellationToken.Register(process.Kill).ToAsyncDisposable();
        await using var _3 = gracefulCancellationToken
            .Register(process.Interrupt)
            .ToAsyncDisposable();

        // Start piping streams in the background
        // With PTY, stderr is merged into stdout, so we only pipe stdin and stdout
        var pipingTask = Task.WhenAll(
            PipePtyInputAsync(process, stdInCts.Token),
            PipePtyOutputAsync(process, stdOutCts.Token)
        );

        try
        {
            await process.WaitUntilExitAsync(waitTimeoutCts.Token).ConfigureAwait(false);
            await stdInCts.CancelAsync();
            // Give output piping a moment to read any remaining data from the pipe buffer
            // before closing the console. The ConPTY might have buffered output.
            await Task.Delay(50).ConfigureAwait(false);
            // Close the PTY console to signal EOF on the output stream.
            // This causes the blocked read in PipePtyOutputAsync to return.
            process.CloseConsole();
            await pipingTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == waitTimeoutCts.Token)
        {
            throw CreateTimeoutException(process, isPty: true, ex);
        }
        catch (OperationCanceledException ex)
            when (ex.CancellationToken == stdInCts.Token || ex.CancellationToken == stdOutCts.Token)
        { }
        catch (OperationCanceledException ex)
            when (ex.CancellationToken == forcefulCancellationToken
                || ex.CancellationToken == gracefulCancellationToken
            ) { }

        ThrowIfCanceled(process, forcefulCancellationToken, gracefulCancellationToken, isPty: true);
        ValidateExitCode(process, isPty: true);

        return new CommandResult(process.ExitCode, process.StartTime, process.ExitTime);
    }

    private async Task<CommandResult> ExecuteAsync(
        ProcessEx process,
        CancellationToken forcefulCancellationToken = default,
        CancellationToken gracefulCancellationToken = default
    )
    {
        using var _ = process;

        // Ideally, we don't want ExecuteAsync() to return or throw before the process actually
        // exits, but it's theoretically possible that an attempt to kill the process may fail,
        // so we need a fallback. This cancellation token is triggered after a timeout once
        // forceful cancellation is requested, and ensures that we don't wait forever.
        using var waitTimeoutCts = new CancellationTokenSource();
        await using var _1 = forcefulCancellationToken
            .Register(() =>
                // ReSharper disable once AccessToDisposedClosure
                waitTimeoutCts.CancelAfter(TimeSpan.FromSeconds(3))
            )
            .ToAsyncDisposable();

        // The process may exit without fully consuming the data from the stdin pipe, in which
        // case we need a separate cancellation signal that will abort the piping operation.
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
            // Output pipe may outlive the process, so don't cancel it on process exit
            // ReSharper disable once PossiblyMistakenUseOfCancellationToken
            PipeStreamAsync(process.StandardOutput, StandardOutputPipe, forcefulCancellationToken),
            // Error pipe may outlive the process, so don't cancel it on process exit
            // ReSharper disable once PossiblyMistakenUseOfCancellationToken
            PipeStreamAsync(process.StandardError, StandardErrorPipe, forcefulCancellationToken)
        );

        try
        {
            // Wait until the process exits normally or gets killed.
            // The timeout is started after the execution is forcefully canceled and ensures
            // that we don't wait forever in case the attempt to kill the process failed.
            await process.WaitUntilExitAsync(waitTimeoutCts.Token).ConfigureAwait(false);

            // Send the cancellation signal to the stdin pipe since the process has exited
            // and won't need it anymore. This should prevent it from hanging in some edge cases.
            await stdInCts.CancelAsync();

            // Wait until piping is done and propagate exceptions
            await pipingTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == waitTimeoutCts.Token)
        {
            // We tried to kill the process, but it didn't exit within the allotted timeout, meaning
            // that the termination attempt failed. This should never happen, but inform the user if it does.
            throw CreateTimeoutException(process, isPty: false, ex);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == stdInCts.Token)
        {
            // This clause will be hit both when stdin piping is canceled due to process exit
            // and when it aborts due to an actual cancellation request (because of the link).
            // Swallow this exception because it was triggered by an internal cancellation,
            // we will throw a more meaningful one later if needed.
        }
        catch (OperationCanceledException ex)
            when (ex.CancellationToken == forcefulCancellationToken
                || ex.CancellationToken == gracefulCancellationToken
            )
        {
            // This clause should never hit due to the registrations above, but just in case it does,
            // swallow the exception here to throw a more meaningful one later.
        }

        ThrowIfCanceled(
            process,
            forcefulCancellationToken,
            gracefulCancellationToken,
            isPty: false
        );
        ValidateExitCode(process, isPty: false);

        return new CommandResult(process.ExitCode, process.StartTime, process.ExitTime);
    }

    /// <summary>
    /// Executes the command asynchronously.
    /// This overload allows you to directly configure the underlying process, and should
    /// only be used in rare cases when you need to break out of the abstraction model
    /// provided by CliWrap.
    /// This overload comes with no warranty and using it may lead to unexpected behavior.
    /// </summary>
    /// <remarks>
    /// This method can be awaited.
    /// </remarks>
    // Added to facilitate running the command without redirecting some/all of the streams
    // https://github.com/Tyrrrz/CliWrap/issues/79
    public CommandTask<CommandResult> ExecuteAsync(
        Action<ProcessStartInfo>? configureStartInfo,
        Action<Process>? configureProcess = null,
        CancellationToken forcefulCancellationToken = default,
        CancellationToken gracefulCancellationToken = default
    )
    {
        var startInfo = CreateStartInfo();
        configureStartInfo?.Invoke(startInfo);

        var process = new ProcessEx(startInfo);

        // This method may fail, and we want to propagate the exceptions immediately instead
        // of wrapping them in a task, so it needs to be executed in a synchronous context.
        // https://github.com/Tyrrrz/CliWrap/issues/139
        process.Start(p =>
        {
            try
            {
                // Disable CA1416 because we're handling an exception that is thrown by the property setters
#pragma warning disable CA1416
                if (ResourcePolicy.Priority is not null)
                    p.PriorityClass = ResourcePolicy.Priority.Value;

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
                    "Cannot start a process with the provided resource policy. "
                        + "Setting custom priority, affinity, and/or working set limits is not supported on this platform.",
                    ex
                );
            }
            catch (InvalidOperationException)
            {
                // This exception could indicate that the process has exited before we had a chance to set the policy.
                // This is not an exceptional situation, so we don't need to do anything here.
            }

            configureProcess?.Invoke(p);
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
    // TODO: (breaking change) use optional parameters and remove the other overload
    public CommandTask<CommandResult> ExecuteAsync(
        CancellationToken forcefulCancellationToken,
        CancellationToken gracefulCancellationToken
    )
    {
        // Check if PTY mode is enabled
        if (PseudoTerminalOptions.IsEnabled)
        {
            return ExecuteWithPtyAsync(forcefulCancellationToken, gracefulCancellationToken);
        }

        return ExecuteAsync(null, null, forcefulCancellationToken, gracefulCancellationToken);
    }

    private CommandTask<CommandResult> ExecuteWithPtyAsync(
        CancellationToken forcefulCancellationToken,
        CancellationToken gracefulCancellationToken
    )
    {
        // Create pseudo-terminal
        var pty = PseudoTerminal.Create(PseudoTerminalOptions.Columns, PseudoTerminalOptions.Rows);

        var process = new PtyProcessEx(
            pty,
            GetOptimallyQualifiedTargetFilePath(),
            Arguments,
            WorkingDirPath,
            CreateEnvironmentBlock()
        );

        process.Start();

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
