using System;
using CliWrap.Buffered;

namespace CliWrap.Immersive;

/// <summary>
/// Result of a command execution, with buffered text data from standard output and standard error streams.
/// Includes additional operator overloads for convenience in immersive mode.
/// </summary>
public partial class ImmersiveCommandResult(
    int exitCode,
    DateTimeOffset startTime,
    DateTimeOffset exitTime,
    string standardOutput,
    string standardError
) : BufferedCommandResult(exitCode, startTime, exitTime, standardOutput, standardError)
{
    /// <summary>
    /// Deconstructs the result into its components.
    /// </summary>
    public void Deconstruct(out int exitCode, out string standardOutput, out string standardError)
    {
        exitCode = ExitCode;
        standardOutput = StandardOutput;
        standardError = StandardError;
    }
}

public partial class ImmersiveCommandResult
{
    /// <summary>
    /// Converts the result to an integer value that corresponds to the <see cref="CommandResult.ExitCode" /> property.
    /// </summary>
    public static implicit operator int(ImmersiveCommandResult result) => result.ExitCode;

    /// <summary>
    /// Converts the result to a boolean value that corresponds to the <see cref="CommandResult.IsSuccess" /> property.
    /// </summary>
    public static implicit operator bool(ImmersiveCommandResult result) => result.IsSuccess;

    /// <summary>
    /// Converts the result to a string value that corresponds to the <see cref="BufferedCommandResult.StandardOutput" /> property.
    /// </summary>
    public static implicit operator string(ImmersiveCommandResult result) => result.StandardOutput;
}
