using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.EventStream
{
    internal class Channel<T> : IDisposable
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);

        private bool _isDisposed;

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public void Publish(T item)
        {
            EnsureNotDisposed();
            _queue.Enqueue(item);
            _semaphore.Release();
        }

        public async Task WaitUntilNextAsync()
        {
            EnsureNotDisposed();
            await _semaphore.WaitAsync();
        }

        public bool TryGetNext(out T result)
        {
            EnsureNotDisposed();
            return _queue.TryDequeue(out result);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            // Release semaphore to free up any awaiting subscribers
            _semaphore.Release(int.MaxValue);
            _semaphore.Dispose();
        }
    }
}