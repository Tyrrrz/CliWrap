using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Utils;
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

            // Used to kill the process if the consumer abandons the iterator or cancels forcefully
            using var forcefulCancellationOrAbandonCts =
                CancellationTokenSource.CreateLinkedTokenSource(forcefulCancellationToken);

            var stdOutPipe = PipeTarget.Merge(
                command.StandardOutputPipe,
                PipeTarget.ToDelegate(
                    async (line, innerCancellationToken) =>
                    {
                        try
                        {
                            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                                innerCancellationToken,
                                forcefulCancellationOrAbandonCts.Token
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
                                && forcefulCancellationOrAbandonCts.IsCancellationRequested
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
                        try
                        {
                            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                                innerCancellationToken,
                                forcefulCancellationOrAbandonCts.Token
                            );

                            await channel
                                .TransmitAsync(new StandardErrorCommandEvent(line), linkedCts.Token)
                                .ConfigureAwait(false);
                        }
                        catch (Exception ex)
                            when ((ex is OperationCanceledException or ObjectDisposedException)
                                && forcefulCancellationOrAbandonCts.IsCancellationRequested
                            )
                        {
                            // The iterator was abandoned during transmit, ignore
                        }
                    },
                    standardErrorEncoding
                )
            );

            // Execute the command with the pipes extended to transmit events to the channel.
            // We pass forcefulCancellationOrAbandonCts.Token as the forceful cancellation token so that abandoning the
            // iterator (which cancels forcefulCancellationOrAbandonCts) also kills the underlying process.
            var commandTask = command
                .WithStandardOutputPipe(stdOutPipe)
                .WithStandardErrorPipe(stdErrPipe)
                .ExecuteAsync(forcefulCancellationOrAbandonCts.Token, gracefulCancellationToken)
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
                            await channel
                                .CloseAsync(forcefulCancellationOrAbandonCts.Token)
                                .ConfigureAwait(false);
                        }
                        catch (Exception ex)
                            when ((ex is OperationCanceledException or ObjectDisposedException)
                                && forcefulCancellationOrAbandonCts.IsCancellationRequested
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
                // breaks out of the iterator early. Cancelling forcefulCancellationOrAbandonCts
                // terminates the underlying process and stops the pipes.
                await forcefulCancellationOrAbandonCts.CancelAsync();

                // Wait for the command to finish executing before returning, observing any
                // exception so it doesn't get reported to the finalizer thread and crash the
                // process. This satisfies the CliWrap convention that the process must be fully
                // terminated by the time the method returns.
                await commandTask.Task.ObserveException().ConfigureAwait(false);
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
