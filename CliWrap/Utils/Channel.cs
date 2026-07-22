using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using PowerKit;

namespace CliWrap.Utils;

internal class Channel<T> : IDisposable
{
    private readonly SemaphoreSlim _transmitLock = new(1, 1);
    private readonly SemaphoreSlim _receiveLock = new(0, 1);

    private readonly Cell<T> _cell = new();

    public async Task TransmitAsync(T item, CancellationToken cancellationToken = default)
    {
        await _transmitLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        _cell.Store(item);

        _receiveLock.Release();
    }

    public async IAsyncEnumerable<T> ReceiveAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        while (true)
        {
            await _receiveLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            if (_cell.TryOpen(out var item))
            {
                yield return item;
                _cell.Clear();
            }
            // If the receive lock was released but the cell is empty,
            // then the channel has been closed.
            else
            {
                break;
            }

            _transmitLock.Release();
        }
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        await _transmitLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        _cell.Clear();

        _receiveLock.Release();
    }

    public void Dispose()
    {
        _transmitLock.Dispose();
        _receiveLock.Dispose();
    }
}
