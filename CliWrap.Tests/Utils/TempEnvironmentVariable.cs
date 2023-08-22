using System;
using System.IO;
using System.Reactive.Disposables;

namespace CliWrap.Tests.Utils;

internal static class TempEnvironmentVariable
{
    public static IDisposable Set(string name, string? value)
    {
        var lastValue = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);

        return Disposable.Create(() => Environment.SetEnvironmentVariable(name, lastValue));
    }

    public static IDisposable ExtendPath(string path) =>
        Set("PATH", Environment.GetEnvironmentVariable("PATH") + Path.PathSeparator + path);
}
