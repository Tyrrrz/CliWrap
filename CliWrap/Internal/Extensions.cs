using System;
using System.Threading.Tasks;

namespace CliWrap.Internal
{
    internal static class Extensions
    {
        public static async Task<TDestination> Select<TSource, TDestination>(this Task<TSource> task, Func<TSource, TDestination> transform)
        {
            var result = await task;
            return transform(result);
        }
    }
}