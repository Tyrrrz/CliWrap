using System.Runtime.CompilerServices;
using CliWrap.Buffered;

namespace CliWrap.Magic;

/// <summary>
/// Extensions for <see cref="CliWrap" /> types.
/// </summary>
public static class CliWrapExtensions
{
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
