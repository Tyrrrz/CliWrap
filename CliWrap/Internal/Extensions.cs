using System;
using System.IO;
using System.Threading.Tasks;

namespace CliWrap.Internal
{
    internal static class Extensions
    {
        public static async Task<TDestination> Select<TSource, TDestination>(this Task<TSource> task, Func<TSource, TDestination> transform)
        {
            var result = await task.ConfigureAwait(false);
            return transform(result);
        }

        public static MemoryStream ToStream(this byte[] data)
        {
            var stream = new MemoryStream();
            stream.Write(data, 0, data.Length);
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }
    }
}