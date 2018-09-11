using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CliWrap.Internal
{
    internal static class Extensions
    {
        public static Stream AsStream(this string str, Encoding encoding)
        {
            var ms = new MemoryStream();
            var data = encoding.GetBytes(str);
            ms.Write(data, 0, data.Length);
            return ms;
        }

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