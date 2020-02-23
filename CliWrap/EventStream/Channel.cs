using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.EventStream
{
    internal class Channel<T> : IDisposable
    {
        private readonly int _capacity;
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);

        private bool _isDisposed;

        public Channel(int capacity)
        {
            _capacity = capacity;
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public void Publish(T item)
        {
            EnsureNotDisposed();

            // This will block the publisher, but, considering that this is used from
            // async event stream, we're okay with blocking command execution since
            // it's taking place on a separate thread anyway.
            if (SpinWait.SpinUntil(() => _queue.Count < _capacity, TimeSpan.FromMinutes(10)))
            {
                _queue.Enqueue(item);

                if (_semaphore.CurrentCount == 0)
                    _semaphore.Release();
            }
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
            _semaphore.Dispose();
        }
    }
}