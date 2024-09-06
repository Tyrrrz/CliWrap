using System;

namespace CliWrap.Utils;

internal class Observable<T>(Func<IObserver<T>, IDisposable> subscribe) : IObservable<T>
{
    public IDisposable Subscribe(IObserver<T> observer) => subscribe(observer);
}

internal static class Observable
{
    public static IObservable<T> Create<T>(Func<IObserver<T>, IDisposable> subscribe) =>
        new Observable<T>(subscribe);

    public static IObservable<T> CreateSynchronized<T>(Func<IObserver<T>, IDisposable> subscribe) =>
        Create<T>(observer => subscribe(new SynchronizedObserver<T>(observer)));
}
