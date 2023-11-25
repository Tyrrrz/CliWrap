using System;

namespace CliWrap.Utils;

internal class SynchronizedObserver<T> : IObserver<T>
{
    private readonly IObserver<T> _observer;
    private readonly object _syncRoot;

    public SynchronizedObserver(IObserver<T> observer, object? syncRoot = null)
    {
        _observer = observer;
        _syncRoot = syncRoot ?? new object();
    }

    public void OnCompleted()
    {
        lock (_syncRoot)
        {
            _observer.OnCompleted();
        }
    }

    public void OnError(Exception error)
    {
        lock (_syncRoot)
        {
            _observer.OnError(error);
        }
    }

    public void OnNext(T value)
    {
        lock (_syncRoot)
        {
            _observer.OnNext(value);
        }
    }
}
