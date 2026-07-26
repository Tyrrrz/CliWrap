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
                // Used to kill the process if the subscription is disposed (i.e. the observable
                // is abandoned) or if forceful cancellation is requested
                var forcefulCancellationOrAbandonCts =
                    CancellationTokenSource.CreateLinkedTokenSource(forcefulCancellationToken);

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

                // Execute the command with the pipes extended to push events to the observer.
                // We pass forcefulCancellationOrAbandonCts.Token as the forceful cancellation token
                // so that abandoning the observable (which cancels forcefulCancellationOrAbandonCts)
                // also kills the underlying process.
                var commandTask = command
                    .WithStandardOutputPipe(stdOutPipe)
                    .WithStandardErrorPipe(stdErrPipe)
                    .ExecuteAsync(
                        forcefulCancellationOrAbandonCts.Token,
                        gracefulCancellationToken
                    );

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
                            // forcefulCancellationOrAbandonCts is linked to the user-provided
                            // forceful cancellation token, so the task's own cancellation token may
                            // be the internal linked one. Surface the user's original token when they
                            // requested forceful cancellation; otherwise (graceful cancellation or an
                            // abandoned observable) fall back to the task's cancellation.
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
                    // The task will remain detached, so observe its exception so it
                    // doesn't get reported to the finalizer thread and crash the process.
                    .Task.ObserveException();

                // When the subscription is disposed (either because the observable was abandoned
                // or because it completed normally), cancel forcefulCancellationOrAbandonCts to
                // terminate the underlying process and stop the pipes. This satisfies the CliWrap
                // convention that the process must be fully terminated once the execution ends.
                // On normal completion the process has already exited, so this is a no-op.
                return Disposable.Create(() =>
                {
                    forcefulCancellationOrAbandonCts.Cancel();
                    forcefulCancellationOrAbandonCts.Dispose();
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
