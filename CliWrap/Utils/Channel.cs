using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Utils
{
    // This is a very simple channel implementation used to convert push-based streams into pull-based.
    // Back-pressure is performed using a write lock. Only one publisher may write at a time.
    // Only one message is buffered and read at a time.

    // Flow:
    // - Write lock is released initially, read lock is not
    // - Consumer waits for read lock
    // - Publisher claims write lock, writes a message, releases a read lock
    // - Consumer goes through, claims read lock, reads one message, releases write lock
    // - Process repeats until the channel transmission is terminated

    internal class Channel<T> : IDisposable where T : class
    {
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private readonly SemaphoreSlim _readLock = new(0, 1);
        private readonly TaskCompletionSource<object?> _closedTcs = new();

        private T? _lastItem;

        public async Task PublishAsync(T item, CancellationToken cancellationToken)
        {
            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            Debug.Assert(_lastItem is null, "Channel overwriting last item.");

            _lastItem = item;
            _readLock.Release();
        }

        public async IAsyncEnumerable<T> ReceiveAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (true)
            {
                var task = await Task.WhenAny(_readLock.WaitAsync(cancellationToken), _closedTcs.Task)
                    .ConfigureAwait(false);

                // Task.WhenAny() does not throw if the underlying task was cancelled.
                // So we check it ourselves and propagate cancellation if it was requested.
                if (task.IsCanceled)
                    await task.ConfigureAwait(false);

                // If the first task to complete was the closing signal, then we will need to break loop.
                // However, WaitAsync() may have completed asynchronously at this point, so we try to
                // read from the queue one last time anyway.
                var isClosed = task == _closedTcs.Task;

                if (_lastItem is not null)
                {
                    yield return _lastItem;
                    _lastItem = null;

                    if (!isClosed)
                        _writeLock.Release();
                }

                if (isClosed)
                    yield break;
            }
        }

        public void Close() => _closedTcs.TrySetResult(null);

        public void Dispose()
        {
            // Can dispose with an item in queue, in case of exception

            Close();
            _writeLock.Dispose();
            _readLock.Dispose();
        }
    }
}