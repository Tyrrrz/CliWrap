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
                        try
                        {
                            observer.OnNext(new StartedCommandEvent(task.ProcessId));

                            var result = await task.ConfigureAwait(false);

                            observer.OnNext(new ExitedCommandEvent(result.ExitCode));
                            observer.OnCompleted();

                            return result;
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                            throw;
                        }
                    });

                // When the consumer unsubscribes from the observable, we trigger a forceful cancellation
                // to terminate the process. If the process has already exited, this will have no effect.
                return Disposable.Create(() =>
                {
                    unsubscribeCts.Cancel();
                    unsubscribeCts.Dispose();

                    // Ideally, the command task should be joined when the consumer unsubscribes from the observable,
                    // but, unlike IAsyncEnumerable<T>, IObservable<T> only provides a synchronous cleanup mechanism.
                    // This leaves us with two choices: either block the thread waiting on the task to complete,
                    // or abandon the task and let it finish in a detached state.
                    // The former can lead to deadlocks in certain scenarios, while the latter can lead to unobserved
                    // exceptions bubbling to the scheduler.
                    // As the lesser of the two evils, we abandon the task and also explicitly observe its exception.
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
