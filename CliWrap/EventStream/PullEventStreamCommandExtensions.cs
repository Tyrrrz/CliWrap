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
        /// <inheritdoc cref="ListenAsync(ICommand, Encoding, Encoding, CancellationToken, CancellationToken)" />
        public IAsyncEnumerable<CommandEvent> ListenAsync(
            Encoding standardOutputEncoding,
            Encoding standardErrorEncoding,
            CancellationToken forcefulCancellationToken,
            CancellationToken gracefulCancellationToken
        ) =>
            ((ICommand)command).ListenAsync(
                standardOutputEncoding,
                standardErrorEncoding,
                forcefulCancellationToken,
                gracefulCancellationToken
            );

        /// <inheritdoc cref="ListenAsync(ICommand, Encoding, Encoding, CancellationToken)" />
        public IAsyncEnumerable<CommandEvent> ListenAsync(
            Encoding standardOutputEncoding,
            Encoding standardErrorEncoding,
            CancellationToken cancellationToken = default
        ) =>
            ((ICommand)command).ListenAsync(
                standardOutputEncoding,
                standardErrorEncoding,
                cancellationToken
            );

        /// <inheritdoc cref="ListenAsync(ICommand, Encoding, CancellationToken)" />
        public IAsyncEnumerable<CommandEvent> ListenAsync(
            Encoding encoding,
            CancellationToken cancellationToken = default
        ) => ((ICommand)command).ListenAsync(encoding, cancellationToken);

        /// <inheritdoc cref="ListenAsync(ICommand, CancellationToken)" />
        public IAsyncEnumerable<CommandEvent> ListenAsync(
            CancellationToken cancellationToken = default
        ) => ((ICommand)command).ListenAsync(cancellationToken);
    }

    /// <inheritdoc cref="EventStreamCommandExtensions" />
    extension(ICommand command)
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

            // Used to trigger forceful cancellation also when the iterator is disposed
            using var forcefulCancellationOrDisposeCts =
                CancellationTokenSource.CreateLinkedTokenSource(forcefulCancellationToken);

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
                .ExecuteAsync(forcefulCancellationOrDisposeCts.Token, gracefulCancellationToken)
                // CommandTask<> doesn't have a method builder, so we wrap it manually to
                // close the channel on completion.
                .Wrap(async task =>
                {
                    try
                    {
                        return await task.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException ex)
                        when (ex.CancellationToken == forcefulCancellationOrDisposeCts.Token
                            && forcefulCancellationToken.IsCancellationRequested
                        )
                    {
                        // Translate the linked cancellation token back to the consumer-provided one
                        throw new OperationCanceledException(
                            ex.Message,
                            ex,
                            forcefulCancellationToken
                        );
                    }
                    finally
                    {
                        try
                        {
                            // Close the channel to release its listeners and finish the loop below
                            await channel
                                .CloseAsync(forcefulCancellationOrDisposeCts.Token)
                                .ConfigureAwait(false);
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
                // The iterator may finish either by reaching its end naturally or by being
                // abandoned (via break, return, or throw). In the latter case, since nothing
                // is listening to the events and draining the pipes anymore, the process may
                // hang indefinitely. Even if it doesn't, we also just don't want it to linger
                // around if the consumer is no longer interested in the events. So to handle that,
                // we trigger a forceful cancellation to terminate the process.
                await forcefulCancellationOrDisposeCts.CancelAsync().ConfigureAwait(false);

                try
                {
                    // Ensure the process never outlives the iterator, even if the iterator was abandoned
                    await commandTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException ex)
                    when (ex.CancellationToken == forcefulCancellationOrDisposeCts.Token
                        && !forcefulCancellationToken.IsCancellationRequested
                    )
                {
                    // Cancellation triggered specifically by the consumer abandoning the iterator.
                    // Don't report internal cancellations.
                }
            }
        }

        /// <inheritdoc cref="ListenAsync(ICommand, Encoding, Encoding, CancellationToken, CancellationToken)" />
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

        /// <inheritdoc cref="ListenAsync(ICommand, Encoding, Encoding, CancellationToken, CancellationToken)" />
        public IAsyncEnumerable<CommandEvent> ListenAsync(
            Encoding encoding,
            CancellationToken cancellationToken = default
        ) => command.ListenAsync(encoding, encoding, cancellationToken);

        /// <inheritdoc cref="ListenAsync(ICommand, Encoding, Encoding, CancellationToken, CancellationToken)" />
        public IAsyncEnumerable<CommandEvent> ListenAsync(
            CancellationToken cancellationToken = default
        ) => command.ListenAsync(Encoding.Default, cancellationToken);
    }
}
