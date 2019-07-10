using System;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Internal
{
    internal class Signal : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);

        public void Release() => _semaphore.Release();

        public void Wait() => _semaphore.Wait();

        public Task WaitAsync() => _semaphore.WaitAsync();

        public void Dispose() => _semaphore.Dispose();
    }
}