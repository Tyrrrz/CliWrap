using System.Runtime.CompilerServices;
using CliWrap.Buffered;

namespace CliWrap.Immersive;

/// <summary>
/// Extensions for <see cref="CliWrap" /> types.
/// </summary>
public static class MagicExtensions
{
    /// <summary>
    /// Executes the command with magic.
    /// </summary>
    public static CommandTask<ImmersiveCommandResult> ExecuteMagicalAsync(this Command command) =>
        command
            .ExecuteBufferedAsync()
            .Select(
                r =>
                    new ImmersiveCommandResult(
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
    public static TaskAwaiter<ImmersiveCommandResult> GetAwaiter(this Command command) =>
        command.ExecuteMagicalAsync().GetAwaiter();
}
