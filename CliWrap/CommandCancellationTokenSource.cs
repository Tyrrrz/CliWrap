using System;
using System.Threading;

namespace CliWrap;

/// <summary>
/// Provides the means to terminate the execution of a command early, either through graceful or forceful means.
/// </summary>
public class CommandCancellationTokenSource : IDisposable
{
    private readonly CancellationTokenSource _gracefulCts = new();
    private readonly CancellationTokenSource _forcefulCts = new();

    /// <summary>
    /// Token associated with this source.
    /// </summary>
    public CommandCancellationToken Token { get; }

    /// <summary>
    /// Initializes an instance of <see cref="CommandCancellationTokenSource" />.
    /// </summary>
    public CommandCancellationTokenSource() =>
        Token = new CommandCancellationToken(_gracefulCts.Token, _forcefulCts.Token);

    /// <summary>
    /// Requests graceful cancellation of an executing command.
    /// This will send an interrupt signal to the underlying process, allowing it to exit on its own terms.
    /// </summary>
    public void CancelGracefully() => _gracefulCts.Cancel();

    /// <summary>
    /// Requests graceful cancellation of an executing command after the specified delay.
    /// This will send an interrupt signal to the underlying process, allowing it to exit on its own terms.
    /// </summary>
    public void CancelGracefullyAfter(TimeSpan delay) => _gracefulCts.CancelAfter(delay);

    /// <summary>
    /// Requests forceful cancellation of an executing command.
    /// This will kill the underlying process.
    /// </summary>
    public void CancelForcefully() => _forcefulCts.Cancel();

    /// <summary>
    /// Requests forceful cancellation of an executing command after the specified delay.
    /// This will kill the underlying process.
    /// </summary>
    public void CancelForcefullyAfter(TimeSpan delay) => _forcefulCts.CancelAfter(delay);

    /// <inheritdoc />
    public void Dispose()
    {
        _gracefulCts.Dispose();
        _forcefulCts.Dispose();
    }
}