using System;

namespace CliWrap.Buffered;

/// <summary>
/// Result of a command execution, with buffered text data from standard output and standard error streams.
/// </summary>
public partial class BufferedCommandResult(
    int exitCode,
    DateTimeOffset startTime,
    DateTimeOffset exitTime,
    string standardOutput,
    string standardError
) : CommandResult(exitCode, startTime, exitTime)
{
    /// <summary>
    /// Standard output data produced by the underlying process.
    /// </summary>
    public string StandardOutput { get; } = standardOutput;

    /// <summary>
    /// Standard error data produced by the underlying process.
    /// </summary>
    public string StandardError { get; } = standardError;

    /// <summary>
    /// Deconstructs the result into its most important components.
    /// </summary>
    public void Deconstruct(out int exitCode, out string standardOutput, out string standardError)
    {
        exitCode = ExitCode;
        standardOutput = StandardOutput;
        standardError = StandardError;
    }
}

public partial class BufferedCommandResult
{
    /// <summary>
    /// Converts the result to a string value that corresponds to the <see cref="BufferedCommandResult.StandardOutput" /> property.
    /// </summary>
    public static implicit operator string(BufferedCommandResult result) => result.StandardOutput;
}
