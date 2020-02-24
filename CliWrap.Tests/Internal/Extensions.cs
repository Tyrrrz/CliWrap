using System.Collections.Generic;
using System.Threading.Tasks;

namespace CliWrap.Tests.Internal
{
    internal static class Extensions
    {
        public static async Task DiscardAsync<T>(this IAsyncEnumerable<T> enumerable)
        {
            await foreach (var _ in enumerable)
            {
                // Do nothing
            }
        }
    }
}