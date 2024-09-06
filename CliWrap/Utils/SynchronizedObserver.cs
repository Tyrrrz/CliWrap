using System;
using System.Threading;

namespace CliWrap.Utils;

internal class SynchronizedObserver<T>(IObserver<T> observer) : IObserver<T>
{
    private readonly Lock _lock = new();

    public void OnCompleted()
    {
        using (_lock.EnterScope())
        {
            observer.OnCompleted();
        }
    }

    public void OnError(Exception error)
    {
        using (_lock.EnterScope())
        {
            observer.OnError(error);
        }
    }

    public void OnNext(T value)
    {
        using (_lock.EnterScope())
        {
            observer.OnNext(value);
        }
    }
}
