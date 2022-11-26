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

    private static readonly Lazy<int> ProcessIdLazy = new(() =>
    {
        using var process = Process.GetCurrentProcess();
        return process.Id;
    });

    public static string? ProcessPath => ProcessPathLazy.Value;

    public static int ProcessId => ProcessIdLazy.Value;
}