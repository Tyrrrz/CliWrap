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
        /// <para>
        /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
        /// </para>
        /// <para>
        /// Unsubscribing from the observable (by calling <see cref="IDisposable.Dispose" /> on the subscription)
        /// will also forcefully terminate the underlying process.
        /// </para>
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
                // Used to trigger forceful cancellation when the consumer unsubscribes from the observable
                var unsubscribeCts = CancellationTokenSource.CreateLinkedTokenSource(
                    forcefulCancellationToken
                );

                var commandTask = command
                    // Extend the existing standard output pipe to also push events to the observer
                    .WithStandardOutputPipe(
                        PipeTarget.Merge(
                            command.StandardOutputPipe,
                            PipeTarget.ToDelegate(
                                line => observer.OnNext(new StandardOutputCommandEvent(line)),
                                standardOutputEncoding
                            )
                        )
                    )
                    // Extend the existing standard error pipe to also push events to the observer
                    .WithStandardErrorPipe(
                        PipeTarget.Merge(
                            command.StandardErrorPipe,
                            PipeTarget.ToDelegate(
                                line => observer.OnNext(new StandardErrorCommandEvent(line)),
                                standardErrorEncoding
                            )
                        )
                    )
                    .ExecuteAsync(unsubscribeCts.Token, gracefulCancellationToken)
                    // Wrap the task to add pre- and post-execution logic
                    .Bind(async task =>
                    {
                        observer.OnNext(new StartedCommandEvent(task.ProcessId));

                        CommandResult result;
                        try
                        {
                            result = await task.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (task.Task.IsCanceled)
                        {
                            observer.OnError(
                                forcefulCancellationToken.IsCancellationRequested
                                    ? new OperationCanceledException(forcefulCancellationToken)
                                    : new TaskCanceledException(task)
                            );
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
                    // The task will remain detached, so observe its exception to prevent it from
                    // routing to the scheduler.
                    .Task.ObserveException();

                // When the consumer unsubscribes from the observable, we trigger a forceful cancellation
                // to terminate the process. If the process has already exited, this will have no effect.
                return Disposable.Create(() =>
                {
                    unsubscribeCts.Cancel();
                    unsubscribeCts.Dispose();
                });
            });

        /// <inheritdoc cref="Observe(Command, Encoding, Encoding, CancellationToken, CancellationToken)" />
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

        /// <inheritdoc cref="Observe(Command, Encoding, Encoding, CancellationToken, CancellationToken)" />
        public IObservable<CommandEvent> Observe(
            Encoding encoding,
            CancellationToken cancellationToken = default
        ) => command.Observe(encoding, encoding, cancellationToken);

        /// <inheritdoc cref="Observe(Command, Encoding, Encoding, CancellationToken, CancellationToken)" />
        public IObservable<CommandEvent> Observe(CancellationToken cancellationToken = default) =>
            command.Observe(Encoding.Default, cancellationToken);
    }
}
