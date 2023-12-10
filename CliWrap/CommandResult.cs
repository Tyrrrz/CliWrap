using System;

namespace CliWrap;

/// <summary>
/// Result of a command execution.
/// </summary>
public class CommandResult(int exitCode, DateTimeOffset startTime, DateTimeOffset exitTime)
{
    /// <summary>
    /// Exit code set by the underlying process.
    /// </summary>
    public int ExitCode { get; } = exitCode;

    /// <summary>
    /// Whether the command execution was successful (i.e. exit code is zero).
    /// </summary>
    public bool IsSuccess => ExitCode == 0;

    /// <summary>
    /// Time at which the command started executing.
    /// </summary>
    public DateTimeOffset StartTime { get; } = startTime;

    /// <summary>
    /// Time at which the command finished executing.
    /// </summary>
    public DateTimeOffset ExitTime { get; } = exitTime;

    /// <summary>
    /// Total duration of the command execution.
    /// </summary>
    public TimeSpan RunTime => ExitTime - StartTime;
}
