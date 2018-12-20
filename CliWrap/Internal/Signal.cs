using System;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Internal
{
    internal class Signal : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);

        public void Release()
        {
            if (_semaphore.CurrentCount <= 0)
                _semaphore.Release();
        }

        public void Wait() => _semaphore.Wait();

        public Task WaitAsync() => _semaphore.WaitAsync();

        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}