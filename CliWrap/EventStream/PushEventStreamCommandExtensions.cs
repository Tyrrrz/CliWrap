using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Utils.Extensions;
using PowerKit;

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

                // Execute the command with the pipes extended to push events to the observer
                var commandTask = command
                    .WithStandardOutputPipe(stdOutPipe)
                    .WithStandardErrorPipe(stdErrPipe)
                    .ExecuteAsync(forcefulCancellationToken, gracefulCancellationToken);

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

                return Disposable.Null;
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
