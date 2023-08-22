using System;

namespace CliWrap.Utils;

file class SynchronizedObserver<T> : IObserver<T>
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

file class Observable<T> : IObservable<T>
{
    private readonly Func<IObserver<T>, IDisposable> _subscribe;

    public Observable(Func<IObserver<T>, IDisposable> subscribe) => _subscribe = subscribe;

    public IDisposable Subscribe(IObserver<T> observer) =>
        _subscribe(new SynchronizedObserver<T>(observer));
}

internal static class Observable
{
    public static IObservable<T> Create<T>(Func<IObserver<T>, IDisposable> subscribe) =>
        new Observable<T>(subscribe);
}
