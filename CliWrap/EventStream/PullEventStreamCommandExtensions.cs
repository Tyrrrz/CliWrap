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
        /// <para>
        /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
        /// </para>
        /// <para>
        /// Abandoning the iterator (via <c>break</c>, <c>return</c>, or <c>throw</c>)
        /// will also forcefully terminate the underlying process.
        /// </para>
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

            // Used to trigger forceful cancellation when the iterator is abandoned
            using var disposeCts = CancellationTokenSource.CreateLinkedTokenSource(
                forcefulCancellationToken
            );

            var commandTask = command
                // Extend the existing standard output pipe to also transmit events to the channel
                .WithStandardOutputPipe(
                    PipeTarget.Merge(
                        command.StandardOutputPipe,
                        PipeTarget.ToDelegate(
                            async (line, innerCancellationToken) =>
                                await channel
                                    .TransmitAsync(
                                        new StandardOutputCommandEvent(line),
                                        innerCancellationToken
                                    )
                                    .ConfigureAwait(false),
                            standardOutputEncoding
                        )
                    )
                )
                // Extend the existing standard error pipe to also transmit events to the channel
                .WithStandardErrorPipe(
                    PipeTarget.Merge(
                        command.StandardErrorPipe,
                        PipeTarget.ToDelegate(
                            async (line, innerCancellationToken) =>
                                await channel
                                    .TransmitAsync(
                                        new StandardErrorCommandEvent(line),
                                        innerCancellationToken
                                    )
                                    .ConfigureAwait(false),
                            standardErrorEncoding
                        )
                    )
                )
                .ExecuteAsync(disposeCts.Token, gracefulCancellationToken)
                // Wrap the task to add pre- and post-execution logic
                .Bind(async task =>
                {
                    try
                    {
                        return await task.ConfigureAwait(false);
                    }
                    finally
                    {
                        try
                        {
                            // Close the channel to release its listeners
                            await channel.CloseAsync(disposeCts.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            // Race condition: the operation was canceled as the channel was closing
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
                // Request forceful cancellation when the iterator is disposed.
                // If the iterator has finished, then the command task has already completed
                // and this cancellation is a no-op.
                // If the iterator was abandoned (via break, return, or throw), then the
                // command task is still running in a detached state, so this cancellation
                // will terminate the process and allow the task to complete.
                await disposeCts.CancelAsync().ConfigureAwait(false);

                // Wait for the task to complete.
                // If it has already been awaited in the try block, then this is a no-op.
                // If the iterator was abandoned, then the above statement should force the task to complete.
                try
                {
                    await commandTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (disposeCts.IsCancellationRequested)
                {
                    // Iterator was abandoned and an internal cancellation was requested
                }
            }
        }

        /// <inheritdoc cref="ListenAsync(Command, Encoding, Encoding, CancellationToken, CancellationToken)" />
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

        /// <inheritdoc cref="ListenAsync(Command, Encoding, Encoding, CancellationToken, CancellationToken)" />
        public IAsyncEnumerable<CommandEvent> ListenAsync(
            Encoding encoding,
            CancellationToken cancellationToken = default
        ) => command.ListenAsync(encoding, encoding, cancellationToken);

        /// <inheritdoc cref="ListenAsync(Command, Encoding, Encoding, CancellationToken, CancellationToken)" />
        public IAsyncEnumerable<CommandEvent> ListenAsync(
            CancellationToken cancellationToken = default
        ) => command.ListenAsync(Encoding.Default, cancellationToken);
    }
}
