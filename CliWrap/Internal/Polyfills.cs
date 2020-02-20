// ReSharper disable CheckNamespace

// Polyfills to bridge the missing APIs in older versions of the framework/standard.

#if NET461 || NETSTANDARD2_0
namespace System.IO
{
    using System.Threading;
    using System.Threading.Tasks;

    internal static class Extensions
    {
        public static async Task CopyToAsync(this Stream source, Stream destination, CancellationToken cancellationToken)
        {
            var buffer = new byte[81920];
            int bytesRead;

            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
#endif

#if NET461
namespace System.Diagnostics
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Management;

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