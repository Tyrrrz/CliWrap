﻿using System.Diagnostics;

namespace CliWrap.Tests.Internal
{
    internal static class ProcessEx
    {
        public static bool IsRunning(int processId)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        }
    }
}