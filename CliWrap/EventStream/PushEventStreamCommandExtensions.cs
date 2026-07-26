using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PowerKit;
using PowerKit.Extensions;

namespace CliWrap.EventStream;

/// <summary>
/// Event stream execution model.
/// </summary>
// TODO: (breaking change) split the partial class into two separate classes, one for each execution model
public static partial class EventStreamCommandExtensions
{
    /// <inheritdoc cref="EventStreamCommandExtensions" />
    extension(Command command)
    {
        /// <summary>
        /// Executes the command as a push-based event stream.
        /// </summary>
        /// <remarks>
        /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
        /// </remarks>
        // TODO: (breaking change) use optional parameters and remove the other overload
        public IObservable<CommandEvent> Observe(
            Encoding standardOutputEncoding,
            Encoding standardErrorEncoding,
            CancellationToken forcefulCancellationToken,
            CancellationToken gracefulCancellationToken
        ) =>
            Observable.CreateSynchronized<CommandEvent>(observer =>
            {
                // Used to kill the process if the subscription is disposed (abandoned)
                // before the command finishes executing.
                var killCts = CancellationTokenSource.CreateLinkedTokenSource(
                    forcefulCancellationToken
                );

                var stdOutPipe = PipeTarget.Merge(
                    command.StandardOutputPipe,
                    PipeTarget.ToDelegate(
                        line => observer.OnNext(new StandardOutputCommandEvent(line)),
                        standardOutputEncoding
                    )
                );

                var stdErrPipe = PipeTarget.Merge(
                    command.StandardErrorPipe,
                    PipeTarget.ToDelegate(
                        line => observer.OnNext(new StandardErrorCommandEvent(line)),
                        standardErrorEncoding
                    )
                );

                // Execute the command with killCts.Token as the forceful cancellation token,
                // so that disposing the subscription (which cancels killCts) also kills the process.
                var commandTask = command
                    .WithStandardOutputPipe(stdOutPipe)
                    .WithStandardErrorPipe(stdErrPipe)
                    .ExecuteAsync(killCts.Token, gracefulCancellationToken);

                observer.OnNext(new StartedCommandEvent(commandTask.ProcessId));

                _ = commandTask
                    .Bind(async task =>
                    {
                        CommandResult result;

                        try
                        {
                            result = await task.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (task.IsCanceled)
                        {
                            observer.OnError(new TaskCanceledException(task));
                            throw;
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                            throw;
                        }

                        // Execute these outside of try/catch to avoid catching exceptions from observer callbacks.
                        // Otherwise, we may get an error event after the completion event.
                        observer.OnNext(new ExitedCommandEvent(result.ExitCode));
                        observer.OnCompleted();

                        return result;
                    })
                    // The task will remain detached, so observe its exception so it
                    // doesn't get reported to the finalizer thread and crash the process.
                    .Task.ObserveException();

                // Return a disposable that cancels killCts to terminate the process if the
                // subscription is disposed before the command completes.
                // This is also triggered on normal completion (OnCompleted/OnError dispose
                // the subscription), but by then the process has already exited, so Kill()
                // is a no-op.
                return Disposable.Create(() =>
                {
                    killCts.Cancel();
                    killCts.Dispose();
                });
            });

        /// <summary>
        /// Executes the command as a push-based event stream.
        /// </summary>
        /// <remarks>
        /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
        /// </remarks>
        public IObservable<CommandEvent> Observe(
            Encoding standardOutputEncoding,
            Encoding standardErrorEncoding,
            CancellationToken cancellationToken = default
        ) =>
            command.Observe(
                standardOutputEncoding,
                standardErrorEncoding,
                cancellationToken,
                CancellationToken.None
            );

        /// <summary>
        /// Executes the command as a push-based event stream.
        /// </summary>
        /// <remarks>
        /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
        /// </remarks>
        public IObservable<CommandEvent> Observe(
            Encoding encoding,
            CancellationToken cancellationToken = default
        ) => command.Observe(encoding, encoding, cancellationToken);

        /// <summary>
        /// Executes the command as a push-based event stream.
        /// Uses <see cref="Encoding.Default" /> for decoding.
        /// </summary>
        /// <remarks>
        /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
        /// </remarks>
        public IObservable<CommandEvent> Observe(CancellationToken cancellationToken = default) =>
            command.Observe(Encoding.Default, cancellationToken);
    }
}
