using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Utils;

internal class Channel<T> : IDisposable
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly SemaphoreSlim _readLock = new(0, 1);

    private bool _isItemAvailable;
    private T _item = default!;

    public async Task PublishAsync(T item, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        _item = item;
        _isItemAvailable = true;

        _readLock.Release();
    }

    public async IAsyncEnumerable<T> ReceiveAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        while (true)
        {
            await _readLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            if (_isItemAvailable)
            {
                yield return _item;
                _isItemAvailable = false;
            }
            // If the read lock was released but the item is not available,
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

        _item = default!;
        _isItemAvailable = false;

        _readLock.Release();
    }

    public void Dispose()
    {
        _writeLock.Dispose();
        _readLock.Dispose();
    }
}
