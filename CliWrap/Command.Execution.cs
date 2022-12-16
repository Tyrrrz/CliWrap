﻿using System;
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
    // In practice, it means that Process.Start("dotnet") works because the corresponding "dotnet.exe"
    // exists on the PATH, but Process.Start("npm") doesn't work because it needs to look for "npm.cmd"
    // instead of "npm.exe". If the extension is provided, however, it works correctly in both cases.
    private string GetOptimallyQualifiedTargetFilePath()
    {
        // Currently, we only need this workaround for script files on Windows,
        // so short-circuit if we are on a different platform.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return TargetFilePath;

        // Don't do anything for fully qualified paths or paths that already have an extension specified.
        // System.Diagnostics.Process knows how to handle those without our help.
        if (Path.IsPathRooted(TargetFilePath) || !string.IsNullOrWhiteSpace(Path.GetExtension(TargetFilePath)))
            return TargetFilePath;

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

            // Directories in the PATH environment variable
            if (Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) is { } paths)
            {
                foreach (var path in paths)
                    yield return path;
            }
        }

        return (
            from probeDirPath in GetProbeDirectoryPaths()
            where Directory.Exists(probeDirPath)
            select Path.Combine(probeDirPath, TargetFilePath)
            into baseFilePath
            from extension in new[] {"exe", "cmd", "bat"}
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
            CreateNoWindow = true
        };

        // Set credentials
        try
        {
            if (Credentials.Domain is not null)
                startInfo.Domain = Credentials.Domain;

            if (Credentials.UserName is not null)
                startInfo.UserName = Credentials.UserName;

            if (Credentials.Password is not null)
                startInfo.Password = Credentials.Password.ToSecureString();

            if (Credentials.LoadUserProfile)
                startInfo.LoadUserProfile = Credentials.LoadUserProfile;
        }
        catch (NotSupportedException ex)
        {
            throw new NotSupportedException(
                "Cannot start a process using provided credentials. " +
                "Setting custom domain, password, or loading user profile is only supported on Windows.",
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
        CancellationToken cancellationToken = default)
    {
        await using (process.StandardInput.ToAsyncDisposable())
        {
            try
            {
                await StandardInputPipe.CopyToAsync(process.StandardInput, cancellationToken)
                    // Some streams do not support cancellation, so we add a fallback that
                    // drops the task and returns early.
                    // This is important with stdin because the process might finish before
                    // the pipe completes, and in case with infinite input stream it would
                    // normally result in a deadlock.
                    .WithUncooperativeCancellation(cancellationToken)
                    .ConfigureAwait(false);
            }
            // Expect IOException: "The pipe has been ended" (Windows) or "Broken pipe" (Unix).
            // Don't catch derived exceptions, such as FileNotFoundException, to avoid false positives.
            // We can't rely on process.HasExited here because of potential race conditions.
            catch (IOException ex) when (ex.GetType() == typeof(IOException))
            {
                // This may happen if the process terminated before the pipe could complete.
                // It's not an exceptional situation because the process may not need
                // the entire stdin to complete successfully.
            }
        }
    }

    private async Task PipeStandardOutputAsync(
        ProcessEx process,
        CancellationToken cancellationToken = default)
    {
        await using (process.StandardOutput.ToAsyncDisposable())
        {
            await StandardOutputPipe.CopyFromAsync(process.StandardOutput, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task PipeStandardErrorAsync(
        ProcessEx process,
        CancellationToken cancellationToken = default)
    {
        await using (process.StandardError.ToAsyncDisposable())
        {
            await StandardErrorPipe.CopyFromAsync(process.StandardError, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<CommandResult> ExecuteAsync(
        ProcessEx process,
        CancellationToken forcefulCancellationToken = default,
        CancellationToken gracefulCancellationToken = default)
    {
        using (process)
        // Additional cancellation for the stdin pipe in case the process terminates early and doesn't fully consume it
        using (var stdInCts = CancellationTokenSource.CreateLinkedTokenSource(forcefulCancellationToken))
        await using (gracefulCancellationToken.Register(process.Interrupt).ToAsyncDisposable())
        await using (forcefulCancellationToken.Register(process.Kill).ToAsyncDisposable())
        {
            // Start piping streams in the background
            var pipingTask = Task.WhenAll(
                PipeStandardInputAsync(process, stdInCts.Token),
                PipeStandardOutputAsync(process, forcefulCancellationToken),
                PipeStandardErrorAsync(process, forcefulCancellationToken)
            );

            // Wait until the process exits normally or gets killed
            await process.WaitUntilExitAsync().ConfigureAwait(false);

            // Send the cancellation signal to the stdin pipe since the process has exited
            // and won't need it anymore.
            // If the pipe has already been exhausted (most likely), this won't do anything.
            // If the pipe is still trying to transfer data, this will cause it to abort.
            stdInCts.Cancel();

            try
            {
                // Wait until piping is done and propagate exceptions
                await pipingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (
                ex.CancellationToken == gracefulCancellationToken ||
                ex.CancellationToken == forcefulCancellationToken ||
                ex.CancellationToken == stdInCts.Token)
            {
                // Cancellations inside pipes are not relevant to the user
            }

            // Throw if forceful cancellation was requested
            // (needs to be checked first because out of the two cancellations this one is the more decisive)
            forcefulCancellationToken.ThrowIfCancellationRequested(
                "Command execution canceled. " +
                $"Process ({process.Name}#{process.Id}) was forcefully terminated."
            );

            // Throw if graceful cancellation was requested
            gracefulCancellationToken.ThrowIfCancellationRequested(
                "Command execution canceled. " +
                $"Process ({process.Name}#{process.Id}) was gracefully terminated."
            );

            // Validate the exit code if required
            if (process.ExitCode != 0 && Validation.IsZeroExitCodeValidationEnabled())
            {
                throw CommandExecutionException.ValidationError(
                    this,
                    process.ExitCode
                );
            }

            return new CommandResult(
                process.ExitCode,
                process.StartTime,
                process.ExitTime
            );
        }
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
        CancellationToken gracefulCancellationToken)
    {
        var process = new ProcessEx(CreateStartInfo());

        // This method may fail and we want to propagate the exceptions immediately
        // instead of wrapping them in a task, so it needs to be executed in a synchronous context.
        // https://github.com/Tyrrrz/CliWrap/issues/139
        process.Start();

        // Extract process ID before calling ExecuteAsync(), because the process may get disposed by then
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