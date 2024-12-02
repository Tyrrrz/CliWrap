using System;
using System.Diagnostics;

namespace CliWrap.Utils;

internal static class EnvironmentEx
{
    private static readonly Lazy<string?> ProcessPathLazy = new(() =>
    {
        using var process = Process.GetCurrentProcess();
        return process.MainModule?.FileName;
    });

    public static string? ProcessPath => ProcessPathLazy.Value;
}
