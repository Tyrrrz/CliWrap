using System.Diagnostics;

namespace CliWrap.Internal
{
    internal static class ProcessExtensions
    {
        public static bool TryKill(this Process process)
        {
            try
            {
                process.Kill();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}