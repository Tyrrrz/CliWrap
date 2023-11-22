using System;

namespace CliWrap;

/// <summary>
/// Result of a command execution.
/// </summary>
public partial class CommandResult
{
    /// <summary>
    /// Exit code set by the underlying process.
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    /// Whether the command execution was successful (i.e. exit code is zero).
    /// </summary>
    public bool IsSuccess => ExitCode == 0;

    /// <summary>
    /// Time at which the command started executing.
    /// </summary>
    public DateTimeOffset StartTime { get; }

    /// <summary>
    /// Time at which the command finished executing.
    /// </summary>
    public DateTimeOffset ExitTime { get; }

    /// <summary>
    /// Total duration of the command execution.
    /// </summary>
    public TimeSpan RunTime => ExitTime - StartTime;

    /// <summary>
    /// Initializes an instance of <see cref="CommandResult" />.
    /// </summary>
    public CommandResult(int exitCode, DateTimeOffset startTime, DateTimeOffset exitTime)
    {
        ExitCode = exitCode;
        StartTime = startTime;
        ExitTime = exitTime;
    }
}

public partial class CommandResult
{
    /// <summary>
    /// Converts the result to an integer value that corresponds to the <see cref="ExitCode" /> property.
    /// </summary>
    // This is only needed by CliWrap.Magic, but we can't implement this operator in another assembly
    public static implicit operator int(CommandResult result) => result.ExitCode;

    /// <summary>
    /// Converts the result to a boolean value that corresponds to the <see cref="IsSuccess" /> property.
    /// </summary>
    // This is only needed by CliWrap.Magic, but we can't implement this operator in another assembly
    public static implicit operator bool(CommandResult result) => result.IsSuccess;
}
