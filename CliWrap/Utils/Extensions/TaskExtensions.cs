using System;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Utils.Extensions;

internal static class TaskExtensions
{
    extension(Task task)
    {
        public Task<AggregateException?> ObserveException() =>
            task.ContinueWith(
                t => t.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted
                    | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );
    }
}
