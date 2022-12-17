using System;

namespace CliWrap.Utils;

file class DelegateDisposable : IDisposable
{
    private readonly Action _dispose;

    public DelegateDisposable(Action dispose) => _dispose = dispose;

    public void Dispose() => _dispose();
}

internal static class Disposable
{
    public static IDisposable Null { get; } = Create(() => { });

    public static IDisposable Create(Action dispose) =>
        new DelegateDisposable(dispose);
}