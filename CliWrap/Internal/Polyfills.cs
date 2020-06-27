// ReSharper disable CheckNamespace

// Polyfills to bridge the missing APIs in older versions of the framework/standard.
// In some cases, these just proxy calls to existing methods but also provide a signature that matches .netstd2.1

#if NET461 || NETSTANDARD2_0
namespace System.IO
{
    using Threading;
    using Threading.Tasks;

    internal static class Extensions
    {
        public static async ValueTask<int> ReadAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken) =>
            await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

        public static async ValueTask<int> ReadAsync(this StreamReader reader, char[] buffer, CancellationToken cancellationToken)
        {
            // StreamReader doesn't accept cancellation token anywhere (pre-netstd2.1)

            cancellationToken.ThrowIfCancellationRequested();
            return await reader.ReadAsync(buffer, 0, buffer.Length);
        }

        public static async ValueTask WriteAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken) =>
            await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);

        public static ValueTask DisposeAsync(this Stream stream)
        {
            stream.Dispose();
            return default;
        }
    }
}
#endif

#if NET461 || NETSTANDARD2_0
namespace System.Collections.Generic
{
    using Diagnostics.CodeAnalysis;

    internal static class Extensions
    {
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
        {
            key = pair.Key;
            value = pair.Value;
        }

        public static bool TryDequeue<T>(this Queue<T> queue, [MaybeNullWhen(false)] out T item)
        {
            if (queue.Count > 0)
            {
                item = queue.Dequeue();
                return true;
            }

            item = default!;
            return false;
        }
    }
}
#endif

#if NET461
namespace System.Diagnostics
{
    using System;
    using Linq;
    using Management;

    internal static class Extensions
    {
        private static void KillProcessTree(int processId)
        {
            using var searcher = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessID={processId}");
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
}
#endif

#if NETSTANDARD2_0 || NETSTANDARD2_1
namespace System.Diagnostics
{
    internal static class Extensions
    {
        // Can't kill children in .NET Standard :(
        public static void Kill(this Process process, bool entireProcessTree) => process.Kill();
    }
}
#endif