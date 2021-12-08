using System.Collections.Generic;
using System.Threading.Tasks;

namespace CliWrap.Tests.Utils.Extensions;

internal static class AsyncExtensions
{
    public static async Task IterateDiscardAsync<T>(this IAsyncEnumerable<T> enumerable)
    {
        await foreach (var _ in enumerable)
        {
            // Do nothing
        }
    }
}