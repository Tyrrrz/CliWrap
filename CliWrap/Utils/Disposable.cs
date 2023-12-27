using System;

namespace CliWrap.Utils;

internal class Disposable(Action dispose) : IDisposable
{
    public static IDisposable Null { get; } = Create(() => { });

    public static IDisposable Create(Action dispose) => new Disposable(dispose);

    public void Dispose() => dispose();
}
