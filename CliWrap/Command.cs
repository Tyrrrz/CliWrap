using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    public IPipeSource StandardInputPipe { get; }

    /// <inheritdoc />
    public IPipeTarget StandardOutputPipe { get; }

    /// <inheritdoc />
    public IPipeTarget StandardErrorPipe { get; }

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
        IPipeSource standardInputPipe,
        IPipeTarget standardOutputPipe,
        IPipeTarget standardErrorPipe)
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
        Pipe.FromNull(),
        Pipe.ToNull(),
        Pipe.ToNull())
    {
    }

    /// <summary>
    /// Creates a copy of this command, setting the arguments to the specified value.
    /// </summary>
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
    public Command WithArguments(IEnumerable<string> arguments, bool escape = true) =>
        WithArguments(args => args.Add(arguments, escape));

    /// <summary>
    /// Creates a copy of this command, setting the arguments to the value configured by the specified delegate.
    /// </summary>
    public Command WithArguments(Action<IArgumentsConfigurator> configure)
    {
        var builder = new ArgumentsBuilder();
        configure(builder);

        return WithArguments(builder.Build());
    }

    /// <summary>
    /// Creates a copy of this command, setting the working directory path to the specified value.
    /// </summary>
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
    public Command WithCredentials(Action<ICredentialsConfigurator> configure)
    {
        var builder = new CredentialsBuilder();
        configure(builder);

        return WithCredentials(builder.Build());
    }

    /// <summary>
    /// Creates a copy of this command, setting the environment variables to the specified value.
    /// </summary>
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
    public Command WithEnvironmentVariables(Action<IEnvironmentVariablesConfigurator> configure)
    {
        var builder = new EnvironmentVariablesBuilder();
        configure(builder);

        return WithEnvironmentVariables(builder.Build());
    }

    /// <summary>
    /// Creates a copy of this command, setting the validation options to the specified value.
    /// </summary>
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
    public Command WithStandardInputPipe(IPipeSource source) => new(
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
    public Command WithStandardOutputPipe(IPipeTarget target) => new(
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
    public Command WithStandardErrorPipe(IPipeTarget target) => new(
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

    // System.Diagnostics.Process already resolves full paths from PATH environment variable,
    // but it only seems to do that for executable files if the extension is omitted.
    // For instance, `Process.Start("dotnet")` works because it can find the "dotnet.exe" file on PATH.
    // On the other hand, `Process.Start("npm")` doesn't work because it needs to find "npm.cmd" instead.
    // However, if we supply the extension too ("npm.cmd" in the sample above), it works correctly.
    // We need to do a bit of extra work to make sure that full paths to script files are also resolved.
    private string ResolveOptimallyQualifiedTargetFilePath()
    {
        // Implementation reference:
        // https://github.com/dotnet/runtime/blob/9a50493f9f1125fda5e2212b9d6718bc7cdbc5c0/src/libraries/System.Diagnostics.Process/src/System/Diagnostics/Process.Unix.cs#L686-L728

        // Currently we only need this workaround for script files on Windows,
        // so short-circuit if we are on a different operating system.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return TargetFilePath;

        // Don't do anything for fully qualified paths or paths that already have an extension specified
        if (Path.IsPathFullyQualified(TargetFilePath) || !string.IsNullOrWhiteSpace(Path.GetExtension(TargetFilePath)))
            return TargetFilePath;

        // Potential directories to look for the file in (ordered by priority)
        var parentPaths = new List<string>();

        // ... executable directory
        if (!string.IsNullOrWhiteSpace(EnvironmentEx.ProcessPath))
        {
            var processDirPath = Path.GetDirectoryName(EnvironmentEx.ProcessPath);
            if (!string.IsNullOrWhiteSpace(processDirPath))
                parentPaths.Add(processDirPath);
        }

        // ... working directory
        parentPaths.Add(Directory.GetCurrentDirectory());

        // ... directories specified in PATH
        parentPaths.AddRange(
            Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ??
            Array.Empty<string>()
        );

        var potentialFilePaths =
            from parentPath in parentPaths
            select Path.Combine(parentPath, TargetFilePath)
            into filePathBase
            from extension in new[] { "exe", "cmd", "bat" }
            select filePathBase + '.' + extension;

        // Return first existing file or fall back to the original path
        return
            potentialFilePaths.FirstOrDefault(File.Exists) ??
            TargetFilePath;
    }

    private ProcessStartInfo GetStartInfo()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveOptimallyQualifiedTargetFilePath(),
            Arguments = Arguments,
            WorkingDirectory = WorkingDirPath,
            UserName = Credentials.UserName ?? "",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Domain and password are only supported on Windows
        if (Credentials.Domain is not null || Credentials.Password is not null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                startInfo.Domain = Credentials.Domain ?? "";
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
            // Workaround for https://github.com/dotnet/runtime/issues/34446
            if (value is null)
            {
                startInfo.Environment.Remove(key);
            }
            else
            {
                startInfo.Environment[key] = value;
            }
        }

        return startInfo;
    }

    private async Task PipeStandardInputAsync(
        ProcessEx process,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Some streams do not support cancellation, so we add a fallback that
            // drops the task and returns early.
            // This is important with stdin because the process might finish before
            // the pipe completes, and in case with infinite input stream it would
            // normally result in a deadlock.
            await StandardInputPipe.CopyToAsync(process.StdIn, cancellationToken)
                .WithUncooperativeCancellation(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (IOException)
        {
            // IOException: The pipe has been ended.
            // This may happen if the process terminated before the pipe could complete.
            // It's not an exceptional situation because the process may not need
            // the entire stdin to complete successfully.
        }
        finally
        {
            await process.StdIn.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task PipeStandardOutputAsync(
        ProcessEx process,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await StandardOutputPipe.CopyFromAsync(process.StdOut, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            await process.StdOut.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task PipeStandardErrorAsync(
        ProcessEx process,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await StandardErrorPipe.CopyFromAsync(process.StdErr, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            await process.StdErr.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task<CommandResult> AttachAsync(
        ProcessEx process,
        CancellationToken cancellationToken = default)
    {
        // Additional cancellation for stdin in case the process terminates early and doesn't fully exhaust it
        using var stdInCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        using var _1 = process;
        await using var _2 = cancellationToken.Register(process.Kill);

        // Start piping streams in the background
        var pipingTask = Task.WhenAll(
            PipeStandardInputAsync(process, stdInCts.Token),
            PipeStandardOutputAsync(process, cancellationToken),
            PipeStandardErrorAsync(process, cancellationToken)
        );

        // Wait until the process is terminated or killed
        await process.WaitUntilExitAsync().ConfigureAwait(false);

        // Send the cancellation signal to the stdin pipe since the process has exited
        // and won't need it anymore.
        // If the pipe has already been exhausted (most likely), this will do nothing.
        // If the pipe is still trying to transfer data, this will cause it to abort.
        stdInCts.Cancel();

        try
        {
            // Wait until piping is done and propagate exceptions
            await pipingTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Don't throw if cancellation happened internally and not by user request
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

    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    /// <remarks>
    /// This method can be awaited.
    /// </remarks>
    public CommandTask<CommandResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var process = new ProcessEx(GetStartInfo());

        // Process must be started in a synchronous context, in order
        // to ensure that, if the process fails to start, the exception
        // will be thrown synchronously and CommandTask won't be created.
        // Otherwise we may end up with a CommandTask that has invalid state.
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