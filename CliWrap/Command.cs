using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Builders;
using CliWrap.Exceptions;
using CliWrap.Utils;
using CliWrap.Utils.Extensions;

namespace CliWrap;

/// <summary>
/// Encapsulates instructions for running a process.
/// </summary>
public partial class Command : ICommandConfiguration
{
    /// <inheritdoc />
    public string TargetFilePath { get; }

    /// <inheritdoc />
    public string Arguments { get; }

    /// <inheritdoc />
    public string WorkingDirPath { get; }

    /// <inheritdoc />
    public Credentials Credentials { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string?> EnvironmentVariables { get; }

    /// <inheritdoc />
    public CommandResultValidation Validation { get; }

    /// <inheritdoc />
    public PipeSource StandardInputPipe { get; }

    /// <inheritdoc />
    public PipeTarget StandardOutputPipe { get; }

    /// <inheritdoc />
    public PipeTarget StandardErrorPipe { get; }

    /// <summary>
    /// Initializes an instance of <see cref="Command"/>.
    /// </summary>
    public Command(
        string targetFilePath,
        string arguments,
        string workingDirPath,
        Credentials credentials,
        IReadOnlyDictionary<string, string?> environmentVariables,
        CommandResultValidation validation,
        PipeSource standardInputPipe,
        PipeTarget standardOutputPipe,
        PipeTarget standardErrorPipe)
    {
        TargetFilePath = targetFilePath;
        Arguments = arguments;
        WorkingDirPath = workingDirPath;
        Credentials = credentials;
        EnvironmentVariables = environmentVariables;
        Validation = validation;
        StandardInputPipe = standardInputPipe;
        StandardOutputPipe = standardOutputPipe;
        StandardErrorPipe = standardErrorPipe;
    }

    /// <summary>
    /// Initializes an instance of <see cref="Command"/>.
    /// </summary>
    public Command(string targetFilePath) : this(
        targetFilePath,
        string.Empty,
        Directory.GetCurrentDirectory(),
        Credentials.Default,
        new Dictionary<string, string?>(),
        CommandResultValidation.ZeroExitCode,
        PipeSource.Null,
        PipeTarget.Null,
        PipeTarget.Null)
    {
    }

    /// <summary>
    /// Creates a copy of this command, setting the arguments to the specified value.
    /// </summary>
    [Pure]
    public Command WithArguments(string arguments) => new(
        TargetFilePath,
        arguments,
        WorkingDirPath,
        Credentials,
        EnvironmentVariables,
        Validation,
        StandardInputPipe,
        StandardOutputPipe,
        StandardErrorPipe
    );

    /// <summary>
    /// Creates a copy of this command, setting the arguments to the value obtained by formatting the specified enumeration.
    /// </summary>
    [Pure]
    public Command WithArguments(IEnumerable<string> arguments, bool escape) =>
        WithArguments(args => args.Add(arguments, escape));

    /// <summary>
    /// Creates a copy of this command, setting the arguments to the value obtained by formatting the specified enumeration.
    /// </summary>
    // TODO: (breaking change) remove in favor of optional parameter
    [Pure]
    public Command WithArguments(IEnumerable<string> arguments) =>
        WithArguments(arguments, true);

    /// <summary>
    /// Creates a copy of this command, setting the arguments to the value configured by the specified delegate.
    /// </summary>
    [Pure]
    public Command WithArguments(Action<ArgumentsBuilder> configure)
    {
        var builder = new ArgumentsBuilder();
        configure(builder);

        return WithArguments(builder.Build());
    }

    /// <summary>
    /// Creates a copy of this command, setting the working directory path to the specified value.
    /// </summary>
    [Pure]
    public Command WithWorkingDirectory(string workingDirPath) => new(
        TargetFilePath,
        Arguments,
        workingDirPath,
        Credentials,
        EnvironmentVariables,
        Validation,
        StandardInputPipe,
        StandardOutputPipe,
        StandardErrorPipe
    );

    /// <summary>
    /// Creates a copy of this command, setting the credentials to the specified value.
    /// </summary>
    [Pure]
    public Command WithCredentials(Credentials credentials) => new(
        TargetFilePath,
        Arguments,
        WorkingDirPath,
        credentials,
        EnvironmentVariables,
        Validation,
        StandardInputPipe,
        StandardOutputPipe,
        StandardErrorPipe
    );

    /// <summary>
    /// Creates a copy of this command, setting the credentials to the value configured by the specified delegate.
    /// </summary>
    [Pure]
    public Command WithCredentials(Action<CredentialsBuilder> configure)
    {
        var builder = new CredentialsBuilder();
        configure(builder);

        return WithCredentials(builder.Build());
    }

    /// <summary>
    /// Creates a copy of this command, setting the environment variables to the specified value.
    /// </summary>
    [Pure]
    public Command WithEnvironmentVariables(IReadOnlyDictionary<string, string?> environmentVariables) => new(
        TargetFilePath,
        Arguments,
        WorkingDirPath,
        Credentials,
        environmentVariables,
        Validation,
        StandardInputPipe,
        StandardOutputPipe,
        StandardErrorPipe
    );

    /// <summary>
    /// Creates a copy of this command, setting the environment variables to the value configured by the specified delegate.
    /// </summary>
    [Pure]
    public Command WithEnvironmentVariables(Action<EnvironmentVariablesBuilder> configure)
    {
        var builder = new EnvironmentVariablesBuilder();
        configure(builder);

        return WithEnvironmentVariables(builder.Build());
    }

    /// <summary>
    /// Creates a copy of this command, setting the validation options to the specified value.
    /// </summary>
    [Pure]
    public Command WithValidation(CommandResultValidation validation) => new(
        TargetFilePath,
        Arguments,
        WorkingDirPath,
        Credentials,
        EnvironmentVariables,
        validation,
        StandardInputPipe,
        StandardOutputPipe,
        StandardErrorPipe
    );

    /// <summary>
    /// Creates a copy of this command, setting the standard input pipe to the specified source.
    /// </summary>
    [Pure]
    public Command WithStandardInputPipe(PipeSource source) => new(
        TargetFilePath,
        Arguments,
        WorkingDirPath,
        Credentials,
        EnvironmentVariables,
        Validation,
        source,
        StandardOutputPipe,
        StandardErrorPipe
    );

    /// <summary>
    /// Creates a copy of this command, setting the standard output pipe to the specified target.
    /// </summary>
    [Pure]
    public Command WithStandardOutputPipe(PipeTarget target) => new(
        TargetFilePath,
        Arguments,
        WorkingDirPath,
        Credentials,
        EnvironmentVariables,
        Validation,
        StandardInputPipe,
        target,
        StandardErrorPipe
    );

    /// <summary>
    /// Creates a copy of this command, setting the standard error pipe to the specified target.
    /// </summary>
    [Pure]
    public Command WithStandardErrorPipe(PipeTarget target) => new(
        TargetFilePath,
        Arguments,
        WorkingDirPath,
        Credentials,
        EnvironmentVariables,
        Validation,
        StandardInputPipe,
        StandardOutputPipe,
        target
    );

    // System.Diagnostics.Process already resolves full path by itself, but it naively assumes that the file
    // is an executable if the extension is omitted. On Windows, BAT and CMD files are also valid targets.
    // In practice, it means that Process.Start("dotnet") works because the corresponding "dotnet.exe"
    // exists on the PATH, but Process.Start("npm") doesn't work because it needs to look for "npm.cmd"
    // instead of "npm.exe". If the extension is provided, however, it works correctly in both cases.
    // This problem is specific to Windows because you can't run scripts directly on other platforms.
    private string GetOptimallyQualifiedTargetFilePath()
    {
        // Currently we only need this workaround for script files on Windows,
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
            from extension in new[] { "exe", "cmd", "bat" }
            select Path.ChangeExtension(baseFilePath, extension)
        ).FirstOrDefault(File.Exists) ?? TargetFilePath;
    }

    private ProcessStartInfo GetStartInfo()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = GetOptimallyQualifiedTargetFilePath(),
            Arguments = Arguments,
            WorkingDirectory = WorkingDirPath,
            UserName = Credentials.UserName,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        // Setting CreateNoWindow adds a 30ms overhead to the execution time of the process.
        // This option only affects console windows and is only relevant if we're running in a process that does not
        // have its own console window already attached. If we're running in a console process, then all child
        // processes will inherit the console window regardless of whether CreateNoWindow is set.
        // This check is only necessary on Windows because CreateNoWindow doesn't work on MacOS or Linux at all.
        // https://github.com/Tyrrrz/CliWrap/pull/142
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && NativeMethods.GetConsoleWindow() == IntPtr.Zero)
            startInfo.CreateNoWindow = true;

        // Domain and password are only supported on Windows
        if (Credentials.Domain is not null || Credentials.Password is not null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                startInfo.Domain = Credentials.Domain;
                startInfo.Password = Credentials.Password?.ToSecureString();
            }
            else
            {
                throw new NotSupportedException(
                    "Cannot start a process using custom domain and/or password on this platform. " +
                    "This feature is only supported on Windows."
                );
            }
        }

        foreach (var (key, value) in EnvironmentVariables)
        {
            if (value is not null)
            {
                startInfo.Environment[key] = value;
            }
            else
            {
                // Workaround for https://github.com/dotnet/runtime/issues/34446
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
                // Some streams do not support cancellation, so we add a fallback that
                // drops the task and returns early.
                // This is important with stdin because the process might finish before
                // the pipe completes, and in case with infinite input stream it would
                // normally result in a deadlock.
                await StandardInputPipe.CopyToAsync(process.StandardInput, cancellationToken)
                    .WithUncooperativeCancellation(cancellationToken)
                    .ConfigureAwait(false);
            }
            // Expect IOException: "The pipe has been ended" (Windows) or "Broken pipe" (Linux).
            // Don't catch derived exceptions, such as FileNotFoundException, to avoid false positives.
            // We can't rely on process.IsExited here because of potential race conditions.
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

    private async Task<CommandResult> AttachAsync(
        ProcessEx process,
        CancellationToken cancellationToken = default)
    {
        using (process)
        // Additional cancellation for stdin in case the process terminates early and doesn't fully consume it
        using (var stdInCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        await using (cancellationToken.Register(process.Kill).ToAsyncDisposable())
        {
            // Start piping streams in the background
            var pipingTask = Task.WhenAll(
                PipeStandardInputAsync(process, stdInCts.Token),
                PipeStandardOutputAsync(process, cancellationToken),
                PipeStandardErrorAsync(process, cancellationToken)
            );

            // Wait until the process exits normally or gets killed
            await process.WaitUntilExitAsync().ConfigureAwait(false);

            // Throw if the process was killed
            if (process.IsKilled)
            {
                // IsKilled is only true if the process was killed by us, so we can safely assume that
                // this happened as a result of user cancellation.
                throw new OperationCanceledException(
                    $"Process (ID: {process.Id}) was killed by user request.",
                    cancellationToken
                );
            }

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
            // Catch cancellations triggered internally
            catch (OperationCanceledException ex) when (ex.CancellationToken == stdInCts.Token)
            {
                // This exception is not critical and has no value to the user, so don't propagate it
            }

            // Validate exit code if required
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
    public CommandTask<CommandResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var process = new ProcessEx(GetStartInfo());

        // Process must be started in a synchronous context in order to ensure that the exception is thrown
        // synchronously in case of failure. Otherwise we may end up with a CommandTask that has invalid state.
        // https://github.com/Tyrrrz/CliWrap/issues/139
        process.Start();

        return new CommandTask<CommandResult>(
            AttachAsync(process, cancellationToken),
            process.Id
        );
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public override string ToString() => $"{TargetFilePath} {Arguments}";
}