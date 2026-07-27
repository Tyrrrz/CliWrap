using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Exceptions;
using CliWrap.Utils;
using PowerKit.Extensions;

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
        if (
            Path.IsPathFullyQualified(TargetFilePath)
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
            if (
                Environment.ProcessPath?.NullIfWhiteSpace() is { } processPath
                && Path.GetDirectoryName(processPath)?.NullIfWhiteSpace() is { } processDirPath
            )
            {
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
            var copyTask = StandardInputPipe.CopyToAsync(process.StandardInput, cancellationToken);

            try
            {
                await copyTask
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // We tried to cancel the copy task and abandoned it. It may remain in a detached
                // state, so we need to observe its exception to prevent it from routing to the scheduler.
                _ = copyTask.ObserveException();

                throw;
            }
            catch (IOException ex)
                // Don't catch derived exceptions, such as FileNotFoundException, to avoid false positives.
                // We also can't rely on process.HasExited here because of potential race conditions.
                when (ex.GetType() == typeof(IOException))
            {
                // Expect IOException: "The pipe has been ended" (Windows) or "Broken pipe" (Unix).
                // This may happen if the process terminated before the pipe has been exhausted.
                // It's not an exceptional situation because the process may not need the entire
                // stdin to complete successfully.
            }
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

        // Used to trigger forceful cancellation if an exception is thrown within this method,
        // for example by the consumer-provided pipes. This ensures that the underlying process
        // never outlives the execution of this method.
        using var panicCts = CancellationTokenSource.CreateLinkedTokenSource(
            forcefulCancellationToken
        );

        // Used to trigger cancellation of the stdin pipe when the process exits. The process may
        // exit without fully consuming all of the stdin data, so we want to avoid waiting for the
        // pipe to finish if there's nothing reading from it anymore.
        using var exitCts = CancellationTokenSource.CreateLinkedTokenSource(panicCts.Token);

        // Kill the process when forceful termination (via cancellation or panic) is requested
        await using var _1 = panicCts.Token.Register(process.Kill).ToAsyncDisposable();

        // Send an interrupt signal to the process when graceful termination is requested
        await using var _2 = gracefulCancellationToken
            .Register(process.Interrupt)
            .ToAsyncDisposable();

        // Start piping streams in the background. In the event that any of the tasks fail,
        // the corresponding standard stream(s) will be closed, so there is no risk of a deadlock.
        // Output and error streams may legally outlive the process, so we don't cancel them when it exits.
        var stdInTask = PipeStandardInputAsync(process, exitCts.Token);
        var stdOutTask = PipeStandardOutputAsync(process, panicCts.Token);
        var stdErrTask = PipeStandardErrorAsync(process, panicCts.Token);

        // Wait until the process exits normally or gets killed
        var processTask = process.WaitUntilExitAsync(
            // In theory, a SIGKILL signal does not guarantee that the process will exit, but even if we
            // abandon it after a timeout, we can't also reliably abandon the pipes since those might not
            // respect cancellation. So we just wait indefinitely and hope for the best.
            CancellationToken.None
        );

        try
        {
            // Check tasks as they complete
            await foreach (
                var completedTask in Task.WhenEach(stdInTask, stdOutTask, stdErrTask, processTask)
            )
            {
                // If the process task finished, trigger the corresponding signal to cancel the stdin pipe
                // since it's definitely not going to be needed anymore.
                if (completedTask == processTask)
                {
                    await exitCts.CancelAsync().ConfigureAwait(false);
                }
                // If a piping task failed while the process is still running, proactively terminate the process.
                // It may continue running for a while even with the corresponding standard stream(s) closed, so
                // we want to cut the wait short since we're going to throw an exception down the line anyway.
                else if (
                    (
                        completedTask == stdInTask
                        || completedTask == stdOutTask
                        || completedTask == stdErrTask
                    )
                    && !completedTask.IsCompletedSuccessfully
                    && !processTask.IsCompleted
                )
                {
                    await panicCts.CancelAsync().ConfigureAwait(false);
                }
            }

            // Join all tasks and propagate exceptions. If any of the tasks faulted, this will throw an aggregation
            // of their exceptions. If some tasks failed while others were cancelled, then the failures take
            // precedence and the cancellation exceptions will be suppressed. If none of the tasks failed but
            // some or all were cancelled, then a single cancellation exception will be thrown.
            await Task.WhenAll(stdInTask, stdOutTask, stdErrTask, processTask)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == exitCts.Token)
        {
            // The stdin pipe was cancelled because the process exited before it could finish writing all data
        }
        catch (OperationCanceledException ex)
            when (ex.CancellationToken == forcefulCancellationToken)
        {
            // The operation was cancelled forcefully by the consumer. In some timing conditions, this exception
            // may never be thrown even if cancellation was requested, so for now we will suppress it and recheck
            // the cancellation token manually later to throw a more meaningful exception.
        }
        catch (OperationCanceledException ex)
            when (ex.CancellationToken == gracefulCancellationToken)
        {
            // This exception is theoretically impossible, seeing as the graceful cancellation token is not passed
            // down anywhere. Still, we have this clause for symmetry.
        }
        finally
        {
            // The process must never outlive the execution of this method
            await processTask.ConfigureAwait(false);
        }

        // Report forceful cancellation
        if (forcefulCancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(
                "Command execution canceled. "
                    + $"Underlying process ({process.Name}#{process.Id}) was forcefully terminated.",
                forcefulCancellationToken
            );
        }

        // Report graceful cancellation
        if (gracefulCancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(
                "Command execution canceled. "
                    + $"Underlying process ({process.Name}#{process.Id}) was gracefully terminated.",
                gracefulCancellationToken
            );
        }

        // Validate the exit code if required
        if (process.ExitCode != 0 && Validation.HasFlag(CommandResultValidation.ZeroExitCode))
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
    /// <inheritdoc cref="ExecuteAsync(CancellationToken, CancellationToken)" />
    /// This overload allows you to directly configure the underlying process, and should
    /// only be used in rare cases when you need to break out of the abstraction model
    /// provided by CliWrap.
    /// This overload comes with no warranty and using it may lead to unexpected behavior.
    /// </summary>
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
    ) => ExecuteAsync(null, null, forcefulCancellationToken, gracefulCancellationToken);

    /// <inheritdoc cref="ExecuteAsync(CancellationToken, CancellationToken)" />
    public CommandTask<CommandResult> ExecuteAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(cancellationToken, CancellationToken.None);
}
