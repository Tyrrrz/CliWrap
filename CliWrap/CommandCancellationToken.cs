using System.Threading;

namespace CliWrap;

/// <summary>
/// Propagates notification that the execution of a command should be canceled.
/// </summary>
public readonly partial struct CommandCancellationToken
{
    /// <summary>
    /// Token that triggers graceful cancellation of a command by
    /// sending an interrupt signal to the underlying process.
    /// </summary>
    public CancellationToken Graceful { get; }

    /// <summary>
    /// Token that triggers forceful cancellation of a command by
    /// killing the underlying process.
    /// </summary>
    public CancellationToken Forceful { get; }

    /// <summary>
    /// Initializes an instance of <see cref="CommandCancellationToken" />.
    /// </summary>
    public CommandCancellationToken(
        CancellationToken graceful,
        CancellationToken forceful)
    {
        Graceful = graceful;
        Forceful = forceful;
    }
}

public partial struct CommandCancellationToken
{
    internal static CommandCancellationToken ForcefulOnly(CancellationToken cancellationToken) =>
        new(CancellationToken.None, cancellationToken);
}