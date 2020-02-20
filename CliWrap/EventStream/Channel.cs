using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.EventStream
{
    internal class Channel<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);

        public void Publish(T item)
        {
            _queue.Enqueue(item);
            _semaphore.Release();
        }

        public async Task WaitUntilNextAsync() => await _semaphore.WaitAsync();

        public bool TryGetNext(out T result) => _queue.TryDequeue(out result);
    }
}