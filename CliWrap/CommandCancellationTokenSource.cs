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
    /// Sends an interrupt signal to the underlying process, requesting it to exit early
    /// but allowing it to do so on its own terms.
    /// Functional equivalent to SIGINT or Ctrl+C.
    /// </summary>
    /// <remarks>
    /// Because this cancellation method is cooperative in nature, it's possible that
    /// the underlying process may choose to ignore it.
    /// In order to ensure that the process exits regardless, for example within an
    /// allotted timeout, you can additionally schedule forceful cancellation
    /// using <see cref="CancelForcefullyAfter" />.
    /// </remarks>
    public void CancelGracefully() => _gracefulCts.Cancel();

    /// <summary>
    /// Schedules an interrupt signal to the underlying process, requesting it to exit early
    /// but allowing it to do so on its own terms.
    /// Functional equivalent to SIGINT or Ctrl+C.
    /// </summary>
    /// <remarks>
    /// Because this cancellation method is cooperative in nature, it's possible that
    /// the underlying process may choose to ignore it.
    /// In order to ensure that the process exits regardless, for example within an
    /// allotted timeout, you can additionally schedule forceful cancellation
    /// using <see cref="CancelForcefullyAfter" />.
    /// </remarks>
    public void CancelGracefullyAfter(TimeSpan delay) => _gracefulCts.CancelAfter(delay);

    /// <summary>
    /// Sends a termination signal to the underlying process, forcing it to exit immediately.
    /// Functional equivalent to SIGTERM or Alt+F4.
    /// </summary>
    public void CancelForcefully() => _forcefulCts.Cancel();

    /// <summary>
    /// Schedules a termination signal to the underlying process, forcing it to exit immediately.
    /// Functional equivalent to SIGTERM or Alt+F4.
    /// </summary>
    public void CancelForcefullyAfter(TimeSpan delay) => _forcefulCts.CancelAfter(delay);

    /// <inheritdoc />
    public void Dispose()
    {
        _gracefulCts.Dispose();
        _forcefulCts.Dispose();
    }
}