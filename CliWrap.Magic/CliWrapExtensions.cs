using System.Runtime.CompilerServices;
using CliWrap.Buffered;

namespace CliWrap.Magic;

/// <summary>
/// Extensions for <see cref="CliWrap" /> types.
/// </summary>
public static class CliWrapExtensions
{
    /// <summary>
    /// Deconstructs the result into its components.
    /// </summary>
    public static void Deconstruct(
        this BufferedCommandResult result,
        out int exitCode,
        out string standardOutput,
        out string standardError
    )
    {
        exitCode = result.ExitCode;
        standardOutput = result.StandardOutput;
        standardError = result.StandardError;
    }

    /// <summary>
    /// Executes the command with magic.
    /// </summary>
    public static CommandTask<MagicalCommandResult> ExecuteMagicalAsync(this Command command) =>
        command
            .ExecuteBufferedAsync()
            .Select(
                r =>
                    new MagicalCommandResult(
                        r.ExitCode,
                        r.StartTime,
                        r.ExitTime,
                        r.StandardOutput,
                        r.StandardError
                    )
            );

    /// <summary>
    /// Executes the command with buffering and returns the awaiter for the result.
    /// </summary>
    public static TaskAwaiter<MagicalCommandResult> GetAwaiter(this Command command) =>
        command.ExecuteMagicalAsync().GetAwaiter();
}
