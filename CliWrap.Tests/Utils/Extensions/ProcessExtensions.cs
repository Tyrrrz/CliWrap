using System.Diagnostics;

namespace CliWrap.Tests.Utils.Extensions;

internal static class ProcessExtensions
{
    extension(Process)
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
