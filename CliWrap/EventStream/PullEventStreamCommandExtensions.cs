using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Utils;

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

            var stdOutPipe = PipeTarget.Merge(
                command.StandardOutputPipe,
                PipeTarget.ToDelegate(
                    async (line, innerCancellationToken) =>
                    {
                        await channel
                            .TransmitAsync(
                                new StandardOutputCommandEvent(line),
                                innerCancellationToken
                            )
                            .ConfigureAwait(false);
                    },
                    standardOutputEncoding
                )
            );

            var stdErrPipe = PipeTarget.Merge(
                command.StandardErrorPipe,
                PipeTarget.ToDelegate(
                    async (line, innerCancellationToken) =>
                    {
                        await channel
                            .TransmitAsync(
                                new StandardErrorCommandEvent(line),
                                innerCancellationToken
                            )
                            .ConfigureAwait(false);
                    },
                    standardErrorEncoding
                )
            );

            // Execute the command with the pipes extended to report events to the channel
            var commandTask = command
                .WithStandardOutputPipe(stdOutPipe)
                .WithStandardErrorPipe(stdErrPipe)
                .ExecuteAsync(forcefulCancellationToken, gracefulCancellationToken);

            yield return new StartedCommandEvent(commandTask.ProcessId);

            // Close the channel when the command finishes executing, so that the consumer can stop listening
            var channelTask = commandTask
                .Task.ContinueWith(
                    async _ =>
                        await channel.CloseAsync(forcefulCancellationToken).ConfigureAwait(false),
                    // Run the continuation even if the parent task failed
                    TaskContinuationOptions.None
                )
                .Unwrap();

            try
            {
                await foreach (
                    var cmdEvent in channel
                        .ReceiveAsync(forcefulCancellationToken)
                        .ConfigureAwait(false)
                )
                {
                    yield return cmdEvent;
                }
            }
            finally
            {
                try
                {
                    // Await the channel task to ensure that the channel is closed and all events are flushed
                    // before the iterator completes and the channel (along with its semaphore) is disposed.
                    // Otherwise, this task may produce an unobserved exception.
                    await channelTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Ignore the internal cancellation exception here as we will throw
                    // a more meaningful one by awaiting the command task below.
                }
            }

            var result = await commandTask.ConfigureAwait(false);
            yield return new ExitedCommandEvent(result.ExitCode);
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
