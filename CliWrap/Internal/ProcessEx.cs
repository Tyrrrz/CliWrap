#if NET45
using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
#endif

namespace CliWrap.Internal
{
    internal static class ProcessEx
    {
#if NET45
        public static void KillProcessTree(int processId)
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
#endif
    }
}