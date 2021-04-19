using System;
using System.Reactive.Disposables;

namespace CliWrap.Tests.Utils
{
    internal static class EnvironmentVariable
    {
        public static IDisposable Set(string name, string? value)
        {
            Environment.SetEnvironmentVariable(name, value);
            return Disposable.Create(() => Environment.SetEnvironmentVariable(name, null));
        }
    }
}