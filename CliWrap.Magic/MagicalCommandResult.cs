using System;
using CliWrap.Buffered;

namespace CliWrap.Magic;

/// <summary>
/// Result of a command execution, with buffered text data from standard output and standard error streams.
/// </summary>
public class MagicalCommandResult : BufferedCommandResult
{
    /// <summary>
    /// Initializes an instance of <see cref="MagicalCommandResult" />.
    /// </summary>
    public MagicalCommandResult(
        int exitCode,
        DateTimeOffset startTime,
        DateTimeOffset exitTime,
        string standardOutput,
        string standardError
    )
        : base(exitCode, startTime, exitTime, standardOutput, standardError) { }

    /// <summary>
    /// Converts the result to an integer value that corresponds to the <see cref="CommandResult.ExitCode" /> property.
    /// </summary>
    public static implicit operator int(MagicalCommandResult result) => result.ExitCode;

    /// <summary>
    /// Converts the result to a boolean value that corresponds to the <see cref="CommandResult.IsSuccess" /> property.
    /// </summary>
    public static implicit operator bool(MagicalCommandResult result) => result.IsSuccess;

    /// <summary>
    /// Converts the result to a string value that corresponds to the <see cref="BufferedCommandResult.StandardOutput" /> property.
    /// </summary>
    public static implicit operator string(MagicalCommandResult result) => result.StandardOutput;
}
