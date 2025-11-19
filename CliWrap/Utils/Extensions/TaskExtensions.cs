using System;
using System.Threading.Tasks;

namespace CliWrap.Utils.Extensions;

internal static class TaskExtensions
{
    extension<TSource>(Task<TSource> task)
    {
        public async Task<TDestination> Select<TDestination>(
            Func<TSource, TDestination> transform
        ) => transform(await task.ConfigureAwait(false));
    }
}
