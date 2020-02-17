using System;

namespace CliWrap.Internal
{
    internal class ChannelPublisher<T> : IDisposable
    {
        private readonly Action<T> _publish;
        private readonly Action _detach;

        public ChannelPublisher(Action<T> publish, Action detach)
        {
            _publish = publish;
            _detach = detach;
        }

        public void Publish(T item) => _publish(item);

        public void Dispose() => _detach();
    }
}