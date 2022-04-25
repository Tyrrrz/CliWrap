using System;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Utils.Extensions;

internal static partial class AsyncExtensions
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

    public static IAsyncDisposable ToAsyncDisposable(this IDisposable disposable) =>
        new AsyncDisposableAdapter(disposable);
}

internal static partial class AsyncExtensions
{
    // Provides a dynamic and uniform way to deal with async disposable.
    // Used as an abstraction to polyfill IAsyncDisposable implementations in BCL types. For example:
    // - Stream in .NET Framework 4.6.1 -> calls Dispose() because DisposeAsync() is not implemented
    // - Stream in .NET Standard 2.0 -> calls DisposeAsync() or Dispose(), depending on actual target framework
    // - Stream in .NET Core 3.0 -> calls DisposeAsync()
    private readonly struct AsyncDisposableAdapter : IAsyncDisposable
    {
        private readonly IDisposable _target;

        public AsyncDisposableAdapter(IDisposable target) => _target = target;

        public async ValueTask DisposeAsync()
        {
            if (_target is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                _target.Dispose();
            }
        }
    }
}