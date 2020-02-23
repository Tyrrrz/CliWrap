using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.EventStream
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

    internal class Channel<T> : IDisposable
    {
        // No need for concurrent queue because we have locks
        private readonly Queue<T> _queue = new Queue<T>(1);
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _readLock = new SemaphoreSlim(0, 1);
        private readonly TaskCompletionSource<object?> _closedTcs = new TaskCompletionSource<object?>();

        private bool _isDisposed;

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public async Task PublishAsync(T item, CancellationToken cancellationToken)
        {
            EnsureNotDisposed();

            await _writeLock.WaitAsync(cancellationToken);
            _queue.Enqueue(item);
            _readLock.Release();
        }

        public async IAsyncEnumerable<T> ReceiveAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            EnsureNotDisposed();

            while (!_isDisposed)
            {
                // Await read lock and completion.
                // If read lock is claimed first, it means the channel is still active and there's a new message.
                // If completion finishes first, it means that the channel is closed, but there still might be one more message
                // in the queue because _readLock.WaitAsync() always yields before finishing.
                var isClosed = _closedTcs.Task == await Task.WhenAny(_readLock.WaitAsync(cancellationToken), _closedTcs.Task);

                if (_queue.TryDequeue(out var next))
                {
                    yield return next;

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
            if (_isDisposed)
                return;

            _isDisposed = true;
            _writeLock.Dispose();
            _readLock.Dispose();
        }
    }
}