using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using PowerKit;

namespace CliWrap.Utils;

internal class Channel<T> : IDisposable
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly SemaphoreSlim _readLock = new(0, 1);

    private readonly Cell<T> _cell = new();

    public async Task PublishAsync(T item, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        _cell.Store(item);

        _readLock.Release();
    }

    public async IAsyncEnumerable<T> ReceiveAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        while (true)
        {
            await _readLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            if (_cell.TryOpen(out var item))
            {
                yield return item;
                _cell.Clear();
            }
            // If the read lock was released but the cell is empty,
            // then the channel has been closed.
            else
            {
                break;
            }

            _writeLock.Release();
        }
    }

    public async Task ReportCompletionAsync(CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        _cell.Clear();

        _readLock.Release();
    }

    public void Dispose()
    {
        _writeLock.Dispose();
        _readLock.Dispose();
    }
}
