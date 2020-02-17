using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CliWrap.Internal
{
    internal static class Extensions
    {
        public static async Task<TDestination> Select<TSource, TDestination>(this Task<TSource> source, Func<TSource, TDestination> transform)
        {
            var result = await source;
            return transform(result);
        }

        public static MemoryStream ToStream(this byte[] data)
        {
            var stream = new MemoryStream();
            stream.Write(data, 0, data.Length);
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        public static ProcessStartInfo SetEnvironmentVariables(this ProcessStartInfo startInfo,
            IReadOnlyDictionary<string, string> envVars)
        {
#if NET45
            foreach (var variable in envVars)
                startInfo.EnvironmentVariables[variable.Key] = variable.Value;
#else
            foreach (var variable in envVars)
                startInfo.Environment[variable.Key] = variable.Value;
#endif

            return startInfo;
        }

        public static async IAsyncEnumerable<string> ReadAllLinesAsync(this StreamReader reader)
        {
            while (!reader.EndOfStream)
                yield return await reader.ReadLineAsync();
        }
    }
}