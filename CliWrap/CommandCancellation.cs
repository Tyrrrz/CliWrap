using System.Threading;

namespace CliWrap;

/// <summary>
/// Cancellation instructions for a command.
/// </summary>
public readonly struct CommandCancellation
{
    /// <summary>
    /// Token that triggers graceful cancellation of a command by sending an interrupt signal to the process.
    /// </summary>
    public CancellationToken GracefulCancellationToken { get; }

    /// <summary>
    /// Token that triggers forceful cancellation of a command by killing the process.
    /// </summary>
    public CancellationToken ForcefulCancellationToken { get; }

    /// <summary>
    /// Initializes an instance of <see cref="CommandCancellation" />.
    /// </summary>
    public CommandCancellation(
        CancellationToken gracefulCancellationToken,
        CancellationToken forcefulCancellationToken)
    {
        GracefulCancellationToken = gracefulCancellationToken;
        ForcefulCancellationToken = forcefulCancellationToken;
    }
}