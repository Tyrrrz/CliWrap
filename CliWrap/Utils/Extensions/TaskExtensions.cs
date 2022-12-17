using System;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Utils.Extensions;

internal static class TaskExtensions
{
    public static async Task<TDestination> Select<TSource, TDestination>(
        this Task<TSource> task,
        Func<TSource, TDestination> transform)
    {
        var result = await task.ConfigureAwait(false);
        return transform(result);
    }

    public static async Task WithUncooperativeCancellation(
        this Task task,
        CancellationToken cancellationToken)
    {
        var cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);

        // Task.WhenAny() doesn't throw if the underlying task wraps an exception
        var finishedTask = await Task.WhenAny(task, cancellationTask).ConfigureAwait(false);

        // Finalize and propagate exceptions
        await finishedTask.ConfigureAwait(false);
    }
}