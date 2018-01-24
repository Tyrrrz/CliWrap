using System.Collections.Generic;
using System.Diagnostics;

namespace CliWrap.Internal
{
    internal static class Extensions
    {
        public static void SetEnvironmentVariables(this ProcessStartInfo startInfo,
            IDictionary<string, string> environmentVariables)
        {
#if NET45
            foreach (var variable in environmentVariables)
                startInfo.EnvironmentVariables.Add(variable.Key, variable.Value);
#else
            foreach (var variable in environmentVariables)
                startInfo.Environment.Add(variable.Key, variable.Value);
#endif
        }

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