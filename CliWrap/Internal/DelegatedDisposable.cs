using System;

namespace CliWrap.Internal
{
    internal class DelegatedDisposable : IDisposable
    {
        private readonly Action _dispose;

        public DelegatedDisposable(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose() => _dispose();
    }
}