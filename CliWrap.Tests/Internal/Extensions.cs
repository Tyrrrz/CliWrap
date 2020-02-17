using System.Collections.Generic;
using System.Threading.Tasks;

namespace CliWrap.Tests.Internal
{
    internal static class Extensions
    {
        public static async Task<IReadOnlyList<T>> AggregateAsync<T>(this IAsyncEnumerable<T> asyncEnumerable)
        {
            var result = new List<T>();

            await foreach (var item in asyncEnumerable)
                result.Add(item);

            return result;
        }
    }
}