using System;

namespace CliWrap.Buffered;

/// <summary>
/// Result of a command execution, with buffered text data from standard output and standard error streams.
/// </summary>
public class BufferedCommandResult(
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
}
