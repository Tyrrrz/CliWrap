using System;
using System.Threading;

namespace CliWrap.Exceptions;

/// <summary>
/// The exception that is thrown when the pipes are cancelled.
/// </summary>
public class PipesCancelledException(
    int exitCode,
    DateTimeOffset startTime,
    DateTimeOffset exitTime,
    string message,
    CancellationToken token
) : OperationCanceledException(message, token)
{
    /// <summary>
    /// Exit code returned by the process.
    /// </summary>
    public int ExitCode { get; } = exitCode;

    /// <summary>
    /// Time when the process started.
    /// </summary>
    public DateTimeOffset StartTime { get; } = startTime;

    /// <summary>
    /// Time when the process exited.
    /// </summary>
    public DateTimeOffset ExitTime { get; } = exitTime;
}
