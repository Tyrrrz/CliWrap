// ReSharper disable CheckNamespace

using System.Diagnostics;

#if NET461
using System;
using System.Management;
using System.Linq;

internal static class ProcessPolyfills
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
internal static class ProcessPolyfills
{
    // Can't kill children in .NET Standard :(
    public static void Kill(this Process process, bool entireProcessTree) => process.Kill();
}
#endif