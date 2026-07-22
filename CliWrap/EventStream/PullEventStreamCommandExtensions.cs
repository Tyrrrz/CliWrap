using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Utils;
using CliWrap.Utils.Extensions;

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
        /// Executes the command as a pull-based event stream.
        /// </summary>
        /// <remarks>
        /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
        /// </remarks>
        // TODO: (breaking change) use optional parameters and remove the other overload
        public async IAsyncEnumerable<CommandEvent> ListenAsync(
            Encoding standardOutputEncoding,
            Encoding standardErrorEncoding,
            [EnumeratorCancellation] CancellationToken forcefulCancellationToken,
            CancellationToken gracefulCancellationToken
        )
        {
            using var channel = new Channel<CommandEvent>();

            // The consumer may abandon the iterator, leaving it in a hanging but uncanceled state.
            // In that case, we want the process to continue running in the background, but we also
            // need to bypass the channel to drain the pipes without waiting for transmit/receive locks.
            using var abandonCts = CancellationTokenSource.CreateLinkedTokenSource(
                forcefulCancellationToken
            );

            var stdOutPipe = PipeTarget.Merge(
                command.StandardOutputPipe,
                PipeTarget.ToDelegate(
                    async (line, innerCancellationToken) =>
                    {
                        // If the iterator was abandoned, then just turn this pipe into a no-op
                        // so that it drains the process's output stream without deadlocking on the channel.
                        if (abandonCts.IsCancellationRequested)
                            return;

                        try
                        {
                            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                                innerCancellationToken,
                                abandonCts.Token
                            );

                            await channel
                                .TransmitAsync(
                                    new StandardOutputCommandEvent(line),
                                    linkedCts.Token
                                )
                                .ConfigureAwait(false);
                        }
                        catch (Exception ex)
                            when ((ex is OperationCanceledException or ObjectDisposedException)
                                && abandonCts.IsCancellationRequested
                            )
                        {
                            // The iterator was abandoned during transmit, ignore
                        }
                    },
                    standardOutputEncoding
                )
            );

            var stdErrPipe = PipeTarget.Merge(
                command.StandardErrorPipe,
                PipeTarget.ToDelegate(
                    async (line, innerCancellationToken) =>
                    {
                        // If the iterator was abandoned, then just turn this pipe into a no-op
                        // so that it drains the process's error stream without deadlocking on the channel.
                        if (abandonCts.IsCancellationRequested)
                            return;

                        try
                        {
                            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                                innerCancellationToken,
                                abandonCts.Token
                            );

                            await channel
                                .TransmitAsync(new StandardErrorCommandEvent(line), linkedCts.Token)
                                .ConfigureAwait(false);
                        }
                        catch (Exception ex)
                            when ((ex is OperationCanceledException or ObjectDisposedException)
                                && abandonCts.IsCancellationRequested
                            )
                        {
                            // The iterator was abandoned during transmit, ignore
                        }
                    },
                    standardErrorEncoding
                )
            );

            // Execute the command with the pipes extended to transmit events to the channel
            var commandTask = command
                .WithStandardOutputPipe(stdOutPipe)
                .WithStandardErrorPipe(stdErrPipe)
                .ExecuteAsync(forcefulCancellationToken, gracefulCancellationToken)
                .Bind(async task =>
                {
                    try
                    {
                        return await task.ConfigureAwait(false);
                    }
                    finally
                    {
                        // Close the channel when the command finishes executing,
                        // so that the consumer can stop listening.
                        try
                        {
                            await channel.CloseAsync(abandonCts.Token).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                            when ((ex is OperationCanceledException or ObjectDisposedException)
                                && abandonCts.IsCancellationRequested
                            )
                        {
                            // The iterator was abandoned as the channel was closing, ignore
                        }
                    }
                });

            try
            {
                yield return new StartedCommandEvent(commandTask.ProcessId);

                await foreach (
                    var cmdEvent in channel
                        .ReceiveAsync(forcefulCancellationToken)
                        .ConfigureAwait(false)
                )
                {
                    yield return cmdEvent;
                }

                var result = await commandTask.ConfigureAwait(false);

                yield return new ExitedCommandEvent(result.ExitCode);
            }
            finally
            {
                // The code after the yield return statements may not execute if the consumer
                // breaks out of the iterator early. Because of that, the pipes will stop
                // draining properly and the execution may deadlock. To avoid that, we trigger
                // a token to stop transmitting events so that the command can keep draining its
                // output without waiting for the consumer to read from the channel.
                await abandonCts.CancelAsync();

                // The task will remain detached, so observe its exception so it
                // doesn't get reported to the finalizer thread and crash the process.
                _ = commandTask.Task.ObserveException();
            }
        }

        /// <summary>
        /// Executes the command as a pull-based event stream.
        /// </summary>
        /// <remarks>
        /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
        /// </remarks>
        public IAsyncEnumerable<CommandEvent> ListenAsync(
            Encoding standardOutputEncoding,
            Encoding standardErrorEncoding,
            CancellationToken cancellationToken = default
        ) =>
            command.ListenAsync(
                standardOutputEncoding,
                standardErrorEncoding,
                cancellationToken,
                CancellationToken.None
            );

        /// <summary>
        /// Executes the command as a pull-based event stream.
        /// </summary>
        /// <remarks>
        /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
        /// </remarks>
        public IAsyncEnumerable<CommandEvent> ListenAsync(
            Encoding encoding,
            CancellationToken cancellationToken = default
        ) => command.ListenAsync(encoding, encoding, cancellationToken);

        /// <summary>
        /// Executes the command as a pull-based event stream.
        /// Uses <see cref="Encoding.Default" /> for decoding.
        /// </summary>
        /// <remarks>
        /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
        /// </remarks>
        public IAsyncEnumerable<CommandEvent> ListenAsync(
            CancellationToken cancellationToken = default
        ) => command.ListenAsync(Encoding.Default, cancellationToken);
    }
}
