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
    /// Sends an interrupt signal to the underlying process, requesting it to terminate early
    /// but allowing it to do so on its own terms.
    /// Functionally equivalent to pressing Ctrl+C in the console window.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Because this cancellation method is cooperative in nature, it's possible that
    ///     the underlying process may choose to ignore the request or take too long to fulfill it.
    ///     In order to ensure that the process exits within an allotted timeout,
    ///     you can additionally schedule forceful cancellation using <see cref="CancelForcefullyAfter" />.
    /// </para>
    /// <para>
    ///     Only supported on Unix. Calling this method on Windows will have no effect.
    /// </para>
    /// </remarks>
    public void CancelGracefully() => _gracefulCts.Cancel();

    /// <summary>
    /// Schedules an interrupt signal to the underlying process, requesting it to terminate early
    /// but allowing it to do so on its own terms.
    /// Functionally equivalent to pressing Ctrl+C in the console window.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Because this cancellation method is cooperative in nature, it's possible that
    ///     the underlying process may choose to ignore the request or take too long to fulfill it.
    ///     In order to ensure that the process exits within an allotted timeout,
    ///     you can additionally schedule forceful cancellation using <see cref="CancelForcefullyAfter" />.
    /// </para>
    /// <para>
    ///     Only supported on Unix. Calling this method on Windows will have no effect.
    /// </para>
    /// </remarks>
    public void CancelGracefullyAfter(TimeSpan delay) => _gracefulCts.CancelAfter(delay);

    /// <summary>
    /// Sends a kill signal to the underlying process, forcing it to terminate immediately.
    /// </summary>
    public void CancelForcefully() => _forcefulCts.Cancel();

    /// <summary>
    /// Schedules a kill signal to the underlying process, forcing it to terminate immediately.
    /// </summary>
    public void CancelForcefullyAfter(TimeSpan delay) => _forcefulCts.CancelAfter(delay);

    /// <inheritdoc />
    public void Dispose()
    {
        _gracefulCts.Dispose();
        _forcefulCts.Dispose();
    }
}