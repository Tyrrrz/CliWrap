using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Utils;

// This is a very simple channel implementation used to convert push-based streams into pull-based ones.
// Flow:
// - Write lock is released initially, read lock is not
// - Consumer waits for the read lock
// - Publisher claims the write lock, writes a message, releases the read lock
// - Consumer goes through, claims read the lock, reads one message, releases the write lock
// - Process repeats until the channel transmission is terminated
internal class Channel<T> : IDisposable
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly SemaphoreSlim _readLock = new(0, 1);
    private readonly TaskCompletionSource<object?> _closedTcs = new();

    private bool _isItemAvailable;
    private T _item = default!;

    public async Task PublishAsync(T item, CancellationToken cancellationToken = default)
    {
        var task = await Task
            .WhenAny(_writeLock.WaitAsync(cancellationToken), _closedTcs.Task)
            .ConfigureAwait(false);

        // Task.WhenAny() does not throw if the underlying task was cancelled.
        // So we check it ourselves and propagate the cancellation if it was requested.
        if (task.IsCanceled)
            await task.ConfigureAwait(false);

        // If the channel closed while waiting for the write lock, throw
        if (task == _closedTcs.Task)
            throw new InvalidOperationException("Channel is closed.");

        _item = item;
        _isItemAvailable = true;
        _readLock.Release();
    }

    public async IAsyncEnumerable<T> ReceiveAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var task = await Task
                .WhenAny(_readLock.WaitAsync(cancellationToken), _closedTcs.Task)
                .ConfigureAwait(false);

            // Task.WhenAny() does not throw if the underlying task was cancelled,
            // so we check it ourselves to propagate the cancellation.
            if (task.IsCanceled)
                await task.ConfigureAwait(false);

            // If the channel closed while waiting for the read lock, yield the item
            // if it's available and then break the loop.
            var isClosed = task == _closedTcs.Task;

            if (_isItemAvailable)
            {
                yield return _item;
                _isItemAvailable = false;
                _writeLock.Release();
            }

            if (isClosed)
                yield break;
        }
    }

    public void Close() => _closedTcs.TrySetResult(null);

    public void Dispose()
    {
        Close();
        _writeLock.Dispose();
        _readLock.Dispose();
    }
}