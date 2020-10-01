using System;
using System.Buffers;

namespace CliWrap.Internal
{
    internal readonly struct PooledBuffer<T> : IDisposable
    {
        public T[] Array { get; }

        public PooledBuffer(int minimumLength) =>
            Array = ArrayPool<T>.Shared.Rent(minimumLength);

        public void Dispose() =>
            ArrayPool<T>.Shared.Return(Array);
    }
}