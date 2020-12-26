using System;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Internal.Extensions
{
    internal static class AsyncExtensions
    {
        public static async Task<TDestination> Select<TSource, TDestination>(
            this Task<TSource> task,
            Func<TSource, TDestination> transform)
        {
            var result = await task.ConfigureAwait(false);
            return transform(result);
        }

        public static async Task WithDangerousCancellation(
            this Task task,
            CancellationToken cancellationToken)
        {
            var cancellationTask = Task.Delay(-1, cancellationToken);

            // Note: Task.WhenAny() doesn't throw
            var finishedTask = await Task.WhenAny(task, cancellationTask).ConfigureAwait(false);

            // Finalize and propagate exceptions
            await finishedTask.ConfigureAwait(false);
        }
    }
}