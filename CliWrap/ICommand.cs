using System.Collections.Generic;
using System.Threading;

namespace CliWrap;

/// <summary>
/// Represents an executable command — common surface shared by <see cref="Command" /> and <see cref="PtyCommand" />.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// File path of the executable, batch file, or script, that this command runs.
    /// </summary>
    string TargetFilePath { get; }

    /// <summary>
    /// Command-line arguments passed to the underlying process.
    /// </summary>
    string Arguments { get; }

    /// <summary>
    /// Working directory path set for the underlying process.
    /// </summary>
    string WorkingDirPath { get; }

    /// <summary>
    /// Environment variables set for the underlying process.
    /// </summary>
    IReadOnlyDictionary<string, string?> EnvironmentVariables { get; }

    /// <summary>
    /// Strategy for validating the result of the execution.
    /// </summary>
    CommandResultValidation Validation { get; }

    /// <summary>
    /// Pipe source for the standard input stream of the underlying process.
    /// </summary>
    PipeSource StandardInputPipe { get; }

    /// <summary>
    /// Pipe target for the standard output stream of the underlying process.
    /// </summary>
    PipeTarget StandardOutputPipe { get; }

    /// <summary>
    /// Pipe target for the standard error stream of the underlying process.
    /// </summary>
    /// <remarks>
    /// For <see cref="PtyCommand" />, this target is fed an empty stream because the
    /// pseudo-terminal merges stderr into stdout.
    /// </remarks>
    PipeTarget StandardErrorPipe { get; }

    /// <summary>
    /// Creates a copy of this command, setting the standard output pipe to the specified target.
    /// </summary>
    ICommand WithStandardOutputPipe(PipeTarget target);

    /// <summary>
    /// Creates a copy of this command, setting the standard error pipe to the specified target.
    /// </summary>
    /// <remarks>
    /// For <see cref="PtyCommand" />, this only affects the empty-stream target signaled
    /// during execution; it has no effect on the actual stderr stream, which the
    /// pseudo-terminal merges into stdout.
    /// </remarks>
    ICommand WithStandardErrorPipe(PipeTarget target);

    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    CommandTask<CommandResult> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    CommandTask<CommandResult> ExecuteAsync(
        CancellationToken forcefulCancellationToken,
        CancellationToken gracefulCancellationToken
    );
}
