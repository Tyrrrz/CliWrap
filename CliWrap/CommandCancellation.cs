using System.Threading;

namespace CliWrap;

/// <summary>
/// Cancellation policy for a command.
/// </summary>
public readonly partial struct CommandCancellation
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

public partial struct CommandCancellation
{
    /// <summary>
    /// Creates cancellation policy for a command by using the provided cancellation token as the
    /// graceful cancellation token. Forceful cancellation will not be used.
    /// </summary>
    public static CommandCancellation GracefulOnly(CancellationToken cancellationToken) =>
        new(cancellationToken, CancellationToken.None);

    /// <summary>
    /// Creates cancellation policy for a command by using the provided cancellation token as the
    /// forceful cancellation token. Graceful cancellation will not be used.
    /// </summary>
    public static CommandCancellation ForcefulOnly(CancellationToken cancellationToken) =>
        new(CancellationToken.None, cancellationToken);
}