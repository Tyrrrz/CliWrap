using System;

namespace CliWrap.Immersive.Utils;

internal partial class Disposable : IDisposable
{
    private readonly Action _dispose;

    public Disposable(Action dispose) => _dispose = dispose;

    public void Dispose() => _dispose();
}

internal partial class Disposable
{
    public static IDisposable Create(Action dispose) => new Disposable(dispose);
}
