using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Internal
{
    internal class Channel<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        private readonly int _capacity;

        private int _publishers;
        public int Publishers => _publishers;

        public Channel(int capacity)
        {
            _capacity = capacity;
        }

        public Channel()
            : this(int.MaxValue)
        {
        }

        public void RegisterListener() => Interlocked.Increment(ref _publishers);

        public void UnregisterListener()
        {
            if (Interlocked.Decrement(ref _publishers) <= 0)
                _semaphore.Release();
        }

        public void Send(T item)
        {
            SpinWait.SpinUntil(() => _queue.Count < _capacity);
            _queue.Enqueue(item);
            _semaphore.Release();
        }

        public async Task WaitUntilNextAsync() => await _semaphore.WaitAsync();

        public bool TryGetNext(out T result) => _queue.TryDequeue(out result);
    }
}