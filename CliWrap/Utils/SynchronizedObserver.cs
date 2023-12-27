using System;

namespace CliWrap.Utils;

internal class SynchronizedObserver<T>(IObserver<T> observer, object? syncRoot = null)
    : IObserver<T>
{
    private readonly object _syncRoot = syncRoot ?? new object();

    public void OnCompleted()
    {
        lock (_syncRoot)
        {
            observer.OnCompleted();
        }
    }

    public void OnError(Exception error)
    {
        lock (_syncRoot)
        {
            observer.OnError(error);
        }
    }

    public void OnNext(T value)
    {
        lock (_syncRoot)
        {
            observer.OnNext(value);
        }
    }
}
