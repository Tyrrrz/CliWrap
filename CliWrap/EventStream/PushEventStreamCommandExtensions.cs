using System;
using System.Text;
using System.Threading;
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
                // Used to trigger forceful cancellation also when the consumer unsubscribes from the observable
                var forcefulCancellationOrUnsubscribeCts =
                    CancellationTokenSource.CreateLinkedTokenSource(forcefulCancellationToken);

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
                    .ExecuteAsync(
                        forcefulCancellationOrUnsubscribeCts.Token,
                        gracefulCancellationToken
                    )
                    // CommandTask<> doesn't have a method builder, so we wrap it manually to
                    // attach observer callbacks for started, exited, and error events.
                    .Wrap(async task =>
                    {
                        observer.OnNext(new StartedCommandEvent(task.ProcessId));

                        CommandResult result;
                        try
                        {
                            result = await task.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException ex)
                            when (ex.CancellationToken == forcefulCancellationOrUnsubscribeCts.Token
                                && forcefulCancellationToken.IsCancellationRequested
                            )
                        {
                            // Translate the linked cancellation token back to the consumer-provided one
                            var translatedEx = new OperationCanceledException(
                                ex.Message,
                                ex,
                                forcefulCancellationToken
                            );

                            observer.OnError(translatedEx);
                            throw translatedEx;
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                            throw;
                        }

                        observer.OnNext(new ExitedCommandEvent(result.ExitCode));
                        observer.OnCompleted();

                        return result;
                    });

                return Disposable.Create(() =>
                {
                    // The observable may finish either by reaching its end naturally or by having
                    // its subscription disposed. In the latter case, since nothing
                    // is listening to the events and draining the pipes anymore, the process may
                    // hang indefinitely. Even if it doesn't, we also just don't want it to linger
                    // around if the consumer is no longer interested in the events. So to handle that,
                    // we trigger a forceful cancellation to terminate the process.
                    forcefulCancellationOrUnsubscribeCts.Cancel();
                    forcefulCancellationOrUnsubscribeCts.Dispose();

                    // Ideally, the command task should be joined when the consumer unsubscribes from the observable,
                    // but, unlike IAsyncEnumerable<T>, IObservable<T> only provides a synchronous cleanup mechanism.
                    // This leaves us with two choices: either block the thread waiting on the task to complete,
                    // or detach the task and let it finish in the background. We take the second option, since blocking
                    // the thread may lead to deadlocks in certain scenarios.
                    _ = commandTask.Task.ObserveException();
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
