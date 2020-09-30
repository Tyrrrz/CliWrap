using System;
using System.Buffers;

namespace CliWrap.Internal
{
    internal struct PooledSharedBuffer<T> : IDisposable
    {
        public T[] Array { get; }
        
        public PooledSharedBuffer(int minimumLength)
        {
            Array = ArrayPool<T>.Shared.Rent(minimumLength);
        }

        public void Dispose()
        {
            ArrayPool<T>.Shared.Return(Array);
        }
    }
}