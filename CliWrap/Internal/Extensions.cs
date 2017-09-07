using System;
using System.Diagnostics;

namespace CliWrap.Internal
{
    internal static class Extensions
    {
        public static bool KillIfRunning(this Process process)
        {
            try
            {
                process.Kill();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }
}