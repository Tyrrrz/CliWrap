using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.EventStream
{
    // This is a very simple channel implementation used to convert push-based streams into pull-based.
    // We can work within our guaranteed constraints:
    // - there will always be exactly 2 publishers and 1 listener.
    // - the publishers and the listener are all on separate threads.

    internal class Channel<T> : IDisposable
    {
        private readonly int _capacity;
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);

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

                // This might release more than one semaphore when there is a race between
                // the two publishers we have. But that's okay, worst case scenario is that
                // the listener will perform an extra cycle, which is better than getting a
                // SemaphoreFullException.
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