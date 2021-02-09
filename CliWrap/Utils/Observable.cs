using System;

namespace CliWrap.Utils
{
    internal class Observable<T> : IObservable<T>
    {
        private readonly Func<IObserver<T>, IDisposable> _subscribe;

        public Observable(Func<IObserver<T>, IDisposable> subscribe) => _subscribe = subscribe;

        public IDisposable Subscribe(IObserver<T> observer) => _subscribe(observer);
    }

    internal static class Observable
    {
        public static IObservable<T> Create<T>(Func<IObserver<T>, IDisposable> subscribe) =>
            new Observable<T>(subscribe);
    }
}