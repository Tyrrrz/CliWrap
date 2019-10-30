using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace CliWrap.Tests.Internal
{
    internal static class ProcessEx
    {
        public static IReadOnlyList<Process> GetChildProcesses(int processId)
        {
            using (var searcher = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessID={processId}"))
            using (var results = searcher.Get())
            {
                return results.Cast<ManagementObject>()
                    .Select(managementObject => Convert.ToInt32(managementObject["ProcessId"]))
                    .Select(Process.GetProcessById)
                    .ToArray();
            }
        }

        public static IReadOnlyList<Process> GetDescendantProcesses(int processId) =>
            GetChildProcesses(processId)
                .SelectMany(p => GetDescendantProcesses(p.Id).Concat(new[] {Process.GetProcessById(p.Id)}))
                .ToArray();
    }
}