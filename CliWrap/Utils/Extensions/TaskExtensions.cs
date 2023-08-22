using System;
using System.Threading.Tasks;

namespace CliWrap.Utils.Extensions;

internal static class TaskExtensions
{
    public static async Task<TDestination> Select<TSource, TDestination>(
        this Task<TSource> task,
        Func<TSource, TDestination> transform
    )
    {
        var result = await task.ConfigureAwait(false);
        return transform(result);
    }
}
