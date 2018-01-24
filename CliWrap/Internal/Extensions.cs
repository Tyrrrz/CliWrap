using System.Diagnostics;

namespace CliWrap.Internal
{
    internal static class Extensions
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