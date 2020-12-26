// ReSharper disable CheckNamespace (global namespace to ensure the extensions are always accessible)

// Polyfills to bridge the missing APIs in older versions of the framework/standard.
// In some cases, these just proxy calls to existing methods but also provide a signature that matches .netstd2.1

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#if NET461
using System.Management;
#endif

#if NET461 || NETSTANDARD2_0
internal static partial class PolyfillExtensions
{
    public static async ValueTask<int> ReadAsync(
        this Stream stream,
        byte[] buffer,
        CancellationToken cancellationToken) =>
        await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

    public static async ValueTask<int> ReadAsync(
        this StreamReader reader,
        char[] buffer,
        CancellationToken cancellationToken)
    {
        // StreamReader doesn't accept cancellation token anywhere (pre-netstd2.1)

        cancellationToken.ThrowIfCancellationRequested();
        return await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
    }

    public static async ValueTask WriteAsync(
        this Stream stream,
        byte[] buffer,
        CancellationToken cancellationToken) =>
        await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

    public static async ValueTask CopyToAsync(
        this Stream stream,
        Stream destination,
        CancellationToken cancellationToken) =>
        await stream.CopyToAsync(destination, 81920, cancellationToken).ConfigureAwait(false);

    public static ValueTask DisposeAsync(this Stream stream)
    {
        stream.Dispose();
        return default;
    }
}
#endif

#if NET461 || NETSTANDARD2_0
internal static partial class PolyfillExtensions
{
    public static void Deconstruct<TKey, TValue>(
        this KeyValuePair<TKey, TValue> pair,
        out TKey key,
        out TValue value)
    {
        key = pair.Key;
        value = pair.Value;
    }
}
#endif

#if NET461
internal static partial class PolyfillExtensions
{
    private static void KillProcessTree(int processId)
    {
        using var searcher = new ManagementObjectSearcher(
            $"Select * From Win32_Process Where ParentProcessID={processId}"
        );

        using var results = searcher.Get();

        // Kill parent process
        try
        {
            using var proc = Process.GetProcessById(processId);
            if (!proc.HasExited)
                proc.Kill();
        }
        catch
        {
            // Do our best and ignore race conditions
        }

        // Kill descendants
        foreach (var managementObject in results.Cast<ManagementObject>())
        {
            var childProcessId = Convert.ToInt32(managementObject["ProcessID"]);
            KillProcessTree(childProcessId);
        }
    }

    public static void Kill(this Process process, bool entireProcessTree)
    {
        if (!entireProcessTree)
            process.Kill();

        KillProcessTree(process.Id);
    }
}
#endif

#if NETSTANDARD2_0 || NETSTANDARD2_1
internal static partial class PolyfillExtensions
{
    // Can't kill children in .NET Standard :(
    public static void Kill(this Process process, bool entireProcessTree) => process.Kill();
}
#endif