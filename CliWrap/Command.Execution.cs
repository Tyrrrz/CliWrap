using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Exceptions;
using CliWrap.Utils.Extensions;
using PowerKit.Extensions;

namespace CliWrap;

public partial class Command
{
    // System.Diagnostics.Process already resolves the full path by itself, but it naively assumes that the file
    // is an executable if the extension is omitted. On Windows, BAT and CMD files may also be valid targets.
    // In practice, it means that Process.Start("foo") will work if it's an EXE file, but will fail if it's a
    // BAT or CMD file, even if it's on the PATH. If the extension is specified, it will work in both cases.
    internal string GetOptimallyQualifiedTargetFilePath()
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

    private async Task PipeStandardInputAsync(
        Process process,
        CancellationToken cancellationToken = default
    )
    {
        if (!process.StartInfo.RedirectStandardInput)
            return;

        await using (process.StandardInput.BaseStream.ToAsyncDisposable())
        {
            var copyTask = StandardInputPipe.CopyToAsync(
                process.StandardInput.BaseStream,
                cancellationToken
            );

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
                // We tried to cancel the copy task, but it may have not cooperated and may still be
                // running in the background. To make sure its exception doesn't bubble up to the scheduler,
                // we explicitly observe it.
                _ = copyTask.Catch();

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
        Process process,
        CancellationToken cancellationToken = default
    )
    {
        if (!process.StartInfo.RedirectStandardOutput)
            return;

        await using (process.StandardOutput.BaseStream.ToAsyncDisposable())
        {
            await StandardOutputPipe
                .CopyFromAsync(process.StandardOutput.BaseStream, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task PipeStandardErrorAsync(
        Process process,
        CancellationToken cancellationToken = default
    )
    {
        if (!process.StartInfo.RedirectStandardError)
            return;

        await using (process.StandardError.BaseStream.ToAsyncDisposable())
        {
            await StandardErrorPipe
                .CopyFromAsync(process.StandardError.BaseStream, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<int> ExecuteAsync(
        Process process,
        CancellationToken forcefulCancellationToken = default,
        CancellationToken gracefulCancellationToken = default
    )
    {
        using var _ = process;

        // Used to trigger forceful cancellation also if an exception is thrown within this method,
        // for example by the consumer-provided pipes. This ensures that the underlying process
        // never outlives the execution of this method, which is one of CliWrap's guarantees.
        using var forcefulCancellationOrPanicCts = CancellationTokenSource.CreateLinkedTokenSource(
            forcefulCancellationToken
        );

        // Used to trigger cancellation of the stdin pipe also when the process exits. The process may
        // exit without fully consuming all of the stdin data, so we want to avoid waiting for the
        // pipe to finish if there's nothing reading from it anymore.
        using var forcefulCancellationOrPanicOrExitCts =
            CancellationTokenSource.CreateLinkedTokenSource(forcefulCancellationOrPanicCts.Token);

        // Kill the process when forceful termination (via cancellation or panic) is requested
        await using var _1 = forcefulCancellationOrPanicCts
            .Token.Register(() => process.TryKill())
            .ToAsyncDisposable();

        // Send an interrupt signal to the process when graceful termination is requested
        await using var _2 = gracefulCancellationToken
            .Register(() => process.TryInterrupt())
            .ToAsyncDisposable();

        // Start piping streams in the background. In the event that any of the tasks fail,
        // the corresponding standard stream(s) will be closed, so there is no risk of a deadlock.
        // Output and error streams may legally outlive the process, so we don't cancel them on exit.
        var stdInTask = PipeStandardInputAsync(process, forcefulCancellationOrPanicOrExitCts.Token);
        var stdOutTask = PipeStandardOutputAsync(process, forcefulCancellationOrPanicCts.Token);
        var stdErrTask = PipeStandardErrorAsync(process, forcefulCancellationOrPanicCts.Token);

        // Wait for the process to exit normally or get killed
        var processTask = process.WaitForExitAsync(
            // We deliberately don't time out here because we don't want to leave a detached running process.
            // If the user wants to end the execution early, they can already do so by triggering cancellation
            // and killing the process. All this timeout would help us with is handle a rare edge case where
            // the kill signal is sent but the process doesn't terminate for some reason. However, we don't
            // really have anything we can do in that situation anyway.
            CancellationToken.None
        );

        try
        {
            // Check on the background tasks as they complete
            await foreach (
                var completedTask in Task.WhenEach(stdInTask, stdOutTask, stdErrTask, processTask)
            )
            {
                // If the process task has finished, trigger the corresponding signal to cancel the
                // stdin pipe task since the process can't read any more data from it.
                if (completedTask == processTask)
                {
                    await forcefulCancellationOrPanicOrExitCts.CancelAsync().ConfigureAwait(false);
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
                    await forcefulCancellationOrPanicCts.CancelAsync().ConfigureAwait(false);
                }
            }

            // Join all tasks and propagate exceptions. If any of the tasks faulted, this will throw an aggregation
            // of their exceptions. If some tasks failed while others were cancelled, then the failures take
            // precedence and the cancellation exceptions will be suppressed. If none of the tasks failed but
            // some or all were cancelled, then a single cancellation exception will be thrown.
            await Task.WhenAll(stdInTask, stdOutTask, stdErrTask, processTask)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
            when (ex.CancellationToken == forcefulCancellationOrPanicCts.Token
                || ex.CancellationToken == forcefulCancellationOrPanicOrExitCts.Token
            )
        {
            // Cancellation was either requested by the consumer or triggered internally. Consumer-initiated
            // cancellations will be reported separately later, while the internal ones shouldn't be reported.
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
                    + $"Underlying process ({process.FileName}#{process.Id}) was forcefully terminated.",
                forcefulCancellationToken
            );
        }

        // Report graceful cancellation
        if (gracefulCancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(
                "Command execution canceled. "
                    + $"Underlying process ({process.FileName}#{process.Id}) was gracefully terminated.",
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
                Command execution failed because the underlying process ({process.FileName}#{process.Id}) returned a non-zero exit code ({process.ExitCode}).

                Command:
                {TargetFilePath} {Arguments}

                You can suppress this validation by calling `{nameof(WithValidation)}({nameof(
                    CommandResultValidation
                )}.{nameof(CommandResultValidation.None)})` on the command.
                """
            );
        }

        return process.ExitCode;
    }

    private CommandTask<CommandResult> ExecuteAsync(
        ProcessStartInfo processStartInfo,
        Action<Process>? configureProcess = null,
        CancellationToken forcefulCancellationToken = default,
        CancellationToken gracefulCancellationToken = default
    )
    {
        var process = new Process { StartInfo = processStartInfo };

        try
        {
            // This method may fail, and we want to propagate the exceptions immediately instead
            // of wrapping them in a task, so it needs to be executed in a synchronous context.
            // https://github.com/Tyrrrz/CliWrap/issues/139
            try
            {
                if (!process.Start())
                {
                    throw new InvalidOperationException(
                        $"Failed to start a process with file path '{process.StartInfo.FileName}'. "
                            + "Target file is not an executable or lacks the 'execute' permission."
                    );
                }
            }
            catch (Win32Exception ex)
            {
                throw new Win32Exception(
                    $"Failed to start a process with file path '{process.StartInfo.FileName}'. "
                        + "Target file or working directory doesn't exist, the provided credentials are invalid, or the resource policy cannot be set due to insufficient permissions. "
                        + "See the inner exception for more information.",
                    ex
                );
            }

            var startTime = DateTimeOffset.Now;

            // Extract this before the process is disposed
            var processId = process.Id;

            // Apply resource policy (must happen after start)
            try
            {
#pragma warning disable CA1416
                if (ResourcePolicy.Priority is not null)
                    process.PriorityClass = ResourcePolicy.Priority.Value;

                if (ResourcePolicy.Affinity is not null)
                    process.ProcessorAffinity = ResourcePolicy.Affinity.Value;

                if (ResourcePolicy.MinWorkingSet is not null)
                    process.MinWorkingSet = ResourcePolicy.MinWorkingSet.Value;

                if (ResourcePolicy.MaxWorkingSet is not null)
                    process.MaxWorkingSet = ResourcePolicy.MaxWorkingSet.Value;
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

            // Apply user-provided configuration (must happen after resource policy)
            configureProcess?.Invoke(process);

            return ExecuteAsync(process, forcefulCancellationToken, gracefulCancellationToken)
                // Convert normal task to our task
                .Pipe(task => new CommandTask<int>(task, processId))
                // Transform the exit code into a proper result object
                .Wrap(async task => new CommandResult(
                    await task.ConfigureAwait(false),
                    startTime,
                    DateTimeOffset.Now
                ));
        }
        catch
        {
            process.Dispose();
            throw;
        }
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
        Action<ProcessStartInfo>? configureProcessStartInfo,
        Action<Process>? configureProcess = null,
        CancellationToken forcefulCancellationToken = default,
        CancellationToken gracefulCancellationToken = default
    )
    {
        var processStartInfo = new ProcessStartInfo
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
                processStartInfo.Domain = Credentials.Domain;

            if (Credentials.UserName is not null)
                processStartInfo.UserName = Credentials.UserName;

            if (Credentials.Password is not null)
                processStartInfo.Password = Credentials.Password.ToSecureString();

            if (Credentials.LoadUserProfile)
                processStartInfo.LoadUserProfile = Credentials.LoadUserProfile;
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
                processStartInfo.Environment[key] = value;
            }
            else
            {
                // Null value means we should remove the variable
                // https://github.com/Tyrrrz/CliWrap/issues/109
                // https://github.com/dotnet/runtime/issues/34446
                processStartInfo.Environment.Remove(key);
            }
        }

        // Apply user-provided configuration
        configureProcessStartInfo?.Invoke(processStartInfo);

        return ExecuteAsync(
            processStartInfo,
            configureProcess,
            forcefulCancellationToken,
            gracefulCancellationToken
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
    ) =>
        ExecuteAsync(
            (Action<ProcessStartInfo>?)null,
            null,
            forcefulCancellationToken,
            gracefulCancellationToken
        );

    /// <inheritdoc cref="ExecuteAsync(CancellationToken, CancellationToken)" />
    public CommandTask<CommandResult> ExecuteAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(cancellationToken, CancellationToken.None);
}
