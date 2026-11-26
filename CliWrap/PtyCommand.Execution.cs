using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Exceptions;
using CliWrap.Utils;
using CliWrap.Utils.Extensions;
using PowerKit.Extensions;

namespace CliWrap;

public partial class PtyCommand
{
    // PTY processes are spawned via CreateProcessW (Windows) or posix_spawn (Unix), not through
    // System.Diagnostics.Process. The same path-resolution caveats from Command apply: a target
    // without an extension on Windows may need explicit probing for .exe/.cmd/.bat.
    private string GetOptimallyQualifiedTargetFilePath()
    {
        if (!OperatingSystem.IsWindows())
            return TargetFilePath;

        if (
            Path.IsPathRooted(TargetFilePath)
            || !string.IsNullOrWhiteSpace(Path.GetExtension(TargetFilePath))
        )
        {
            return TargetFilePath;
        }

        static IEnumerable<string> GetProbeDirectoryPaths()
        {
            if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
            {
                var processDirPath = Path.GetDirectoryName(Environment.ProcessPath);
                if (!string.IsNullOrWhiteSpace(processDirPath))
                    yield return processDirPath;
            }

            yield return Directory.GetCurrentDirectory();

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

    private string? CreateEnvironmentBlock()
    {
        if (EnvironmentVariables.Count == 0)
            return null;

        var comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var env = new SortedDictionary<string, string?>(comparer);

        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            env[(string)entry.Key] = (string?)entry.Value;
        }

        if (!OperatingSystem.IsWindows())
            env["TERM"] = "xterm-256color";

        foreach (var (key, value) in EnvironmentVariables)
        {
            if (value is not null)
                env[key] = value;
            else
                env.Remove(key);
        }

        return string.Join("\0", env.Select(kv => $"{kv.Key}={kv.Value}")) + "\0";
    }

    private async Task PipeStandardInputAsync(
        PtyProcessEx process,
        CancellationToken cancellationToken = default
    )
    {
        // IMPORTANT: Do NOT dispose the stdin stream here.
        // With PTY, closing the input pipe causes ConPTY to send Ctrl+C to the process.
        // The stream is cleaned up when the PTY is disposed after the process exits.
        try
        {
            var copyTask = StandardInputPipe.CopyToAsync(process.StandardInput, cancellationToken);

            try
            {
                await copyTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // A consumer-provided source may ignore cancellation. Observe it in the
                // background so process exit never waits indefinitely for that source.
                _ = copyTask.Catch();
                throw;
            }

            await process.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        // Expect IOException: pipe ended (Windows) or broken pipe (Unix), and ObjectDisposedException
        // when the stream gets disposed during process teardown. OperationCanceledException must
        // propagate so the execute loop can throw a meaningful error.
        catch (Exception ex)
            when (ex.GetType() == typeof(IOException)
                || ex.GetType() == typeof(ObjectDisposedException)
            ) { }
    }

    private async Task PipeStandardOutputAsync(
        PtyProcessEx process,
        CancellationToken cancellationToken = default
    )
    {
        await StandardOutputPipe
            .CopyFromAsync(process.StandardOutput, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task PipeStandardErrorAsync(CancellationToken cancellationToken = default)
    {
        // PTY merges stderr into stdout, so the configured stderr pipe receives an empty stream
        // rather than being left idle.
        await ((ICommand)this)
            .StandardErrorPipe.CopyFromAsync(Stream.Null, cancellationToken)
            .ConfigureAwait(false);
    }

    private void ThrowIfCanceled(
        PtyProcessEx process,
        CancellationToken forcefulCancellationToken,
        CancellationToken gracefulCancellationToken
    )
    {
        if (forcefulCancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(
                $"Command execution canceled. Underlying process ({process.Name}#{process.Id}) was forcefully terminated.",
                forcefulCancellationToken
            );
        }

        if (gracefulCancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(
                $"Command execution canceled. Underlying process ({process.Name}#{process.Id}) was gracefully terminated.",
                gracefulCancellationToken
            );
        }
    }

    private void ValidateExitCode(PtyProcessEx process)
    {
        if (process.ExitCode != 0 && Validation.HasFlag(CommandResultValidation.ZeroExitCode))
        {
            // CommandExecutionException expects an ICommandConfiguration; PtyCommand doesn't
            // implement it, so we wrap with an adapter for diagnostics purposes only.
            throw new CommandExecutionException(
                new PtyCommandConfigurationAdapter(this),
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
    }

    private TimeoutException CreateTimeoutException(PtyProcessEx process, Exception inner) =>
        new(
            $"Failed to terminate the underlying process ({process.Name}#{process.Id}) within the allotted timeout.",
            inner
        );

    private async Task<CommandResult> ExecuteAsync(
        PtyProcessEx process,
        CancellationToken forcefulCancellationToken = default,
        CancellationToken gracefulCancellationToken = default
    )
    {
        using var processScope = process;

        // Used to trigger forceful cancellation also when a consumer-provided pipe fails.
        // This preserves CliWrap's guarantee that a process cannot outlive its execution task.
        using var forcefulCancellationOrPanicCts = CancellationTokenSource.CreateLinkedTokenSource(
            forcefulCancellationToken
        );

        using var waitTimeoutCts = new CancellationTokenSource();
        await using var _1 = forcefulCancellationToken
            .Register(() => waitTimeoutCts.CancelAfter(TimeSpan.FromSeconds(3)))
            .ToAsyncDisposable();

        // The process may exit without fully consuming stdin, in which case we abort the piping
        // operation. Pipe failures also cancel it through the panic token.
        using var forcefulCancellationOrPanicOrExitCts =
            CancellationTokenSource.CreateLinkedTokenSource(forcefulCancellationOrPanicCts.Token);

        await using var _2 = forcefulCancellationOrPanicCts
            .Token.Register(process.Kill)
            .ToAsyncDisposable();
        await using var _3 = gracefulCancellationToken
            .Register(process.Interrupt)
            .ToAsyncDisposable();

        var stdInTask = PipeStandardInputAsync(process, forcefulCancellationOrPanicOrExitCts.Token);
        var stdOutTask = PipeStandardOutputAsync(process, forcefulCancellationOrPanicCts.Token);
        var stdErrTask = PipeStandardErrorAsync(forcefulCancellationOrPanicCts.Token);
        var pipingTask = Task.WhenAll(stdInTask, stdOutTask, stdErrTask);
        var processTask = process.WaitUntilExitAsync(waitTimeoutCts.Token);

        try
        {
            await foreach (
                var completedTask in Task.WhenEach(stdInTask, stdOutTask, stdErrTask, processTask)
            )
            {
                if (completedTask == processTask)
                {
                    await forcefulCancellationOrPanicOrExitCts.CancelAsync().ConfigureAwait(false);

                    // Propagate the termination timeout immediately instead of waiting for
                    // output tasks that may still be blocked on the unresponsive process.
                    if (!processTask.IsCompletedSuccessfully)
                        await processTask.ConfigureAwait(false);
                }
                else if (!completedTask.IsCompletedSuccessfully && !processTask.IsCompleted)
                {
                    await forcefulCancellationOrPanicCts.CancelAsync().ConfigureAwait(false);
                }
            }

            await Task.WhenAll(stdInTask, stdOutTask, stdErrTask, processTask)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == waitTimeoutCts.Token)
        {
            _ = pipingTask.Catch();
            throw CreateTimeoutException(process, ex);
        }
        catch (OperationCanceledException ex)
            when (ex.CancellationToken == forcefulCancellationOrPanicCts.Token
                || ex.CancellationToken == forcefulCancellationOrPanicOrExitCts.Token
            )
        {
            // Consumer cancellation is translated below; internal panic/exit cancellation
            // should not replace a pipe exception or successful process result.
        }

        ThrowIfCanceled(process, forcefulCancellationToken, gracefulCancellationToken);
        ValidateExitCode(process);

        return new CommandResult(process.ExitCode, process.StartTime, process.ExitTime);
    }

    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    /// <remarks>
    /// This method can be awaited.
    /// </remarks>
    public CommandTask<CommandResult> ExecuteAsync(
        CancellationToken forcefulCancellationToken,
        CancellationToken gracefulCancellationToken
    )
    {
        var pty = PseudoTerminal.Create(Columns, Rows);

        var process = new PtyProcessEx(
            pty,
            GetOptimallyQualifiedTargetFilePath(),
            Arguments,
            WorkingDirPath,
            CreateEnvironmentBlock()
        );

        try
        {
            process.Start();
        }
        catch
        {
            process.Dispose();
            throw;
        }

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

internal sealed class PtyCommandConfigurationAdapter(PtyCommand command) : ICommandConfiguration
{
    public string TargetFilePath => command.TargetFilePath;
    public string Arguments => command.Arguments;
    public string WorkingDirPath => command.WorkingDirPath;
    public ResourcePolicy ResourcePolicy => ResourcePolicy.Default;
    public Credentials Credentials => Credentials.Default;
    public IReadOnlyDictionary<string, string?> EnvironmentVariables =>
        command.EnvironmentVariables;
    public CommandResultValidation Validation => command.Validation;
    public PipeSource StandardInputPipe => command.StandardInputPipe;
    public PipeTarget StandardOutputPipe => command.StandardOutputPipe;
    public PipeTarget StandardErrorPipe => ((ICommand)command).StandardErrorPipe;
}
