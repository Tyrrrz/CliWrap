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
    /// <summary>
    /// Executes the command as a pull-based event stream.
    /// </summary>
    /// <remarks>
    /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
    /// </remarks>
    // TODO: (breaking change) use optional parameters and remove the other overload
    public static async IAsyncEnumerable<CommandEvent> ListenAsync(
        this Command command,
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
                    // ReSharper disable once AccessToDisposedClosure
                    await channel
                        .PublishAsync(new StandardOutputCommandEvent(line), innerCancellationToken)
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
                    // ReSharper disable once AccessToDisposedClosure
                    await channel
                        .PublishAsync(new StandardErrorCommandEvent(line), innerCancellationToken)
                        .ConfigureAwait(false);
                },
                standardErrorEncoding
            )
        );

        var commandWithPipes = command
            .WithStandardOutputPipe(stdOutPipe)
            .WithStandardErrorPipe(stdErrPipe);

        var commandTask = commandWithPipes.ExecuteAsync(
            forcefulCancellationToken,
            gracefulCancellationToken
        );

        yield return new StartedCommandEvent(commandTask.ProcessId);

        // Close the channel once the command completes, so that ReceiveAsync() can finish
        _ = commandTask.Task.ContinueWith(
            async _ =>
                // ReSharper disable once AccessToDisposedClosure
                await channel
                    .ReportCompletionAsync(forcefulCancellationToken)
                    .ConfigureAwait(false),
            // Run the continuation even if the parent task failed
            TaskContinuationOptions.None
        );

        await foreach (
            var cmdEvent in channel.ReceiveAsync(forcefulCancellationToken).ConfigureAwait(false)
        )
        {
            yield return cmdEvent;
        }

        var exitCode = await commandTask.Select(r => r.ExitCode).ConfigureAwait(false);
        yield return new ExitedCommandEvent(exitCode);
    }

    /// <summary>
    /// Executes the command as a pull-based event stream.
    /// </summary>
    /// <remarks>
    /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
    /// </remarks>
    public static IAsyncEnumerable<CommandEvent> ListenAsync(
        this Command command,
        Encoding standardOutputEncoding,
        Encoding standardErrorEncoding,
        CancellationToken cancellationToken = default
    )
    {
        return command.ListenAsync(
            standardOutputEncoding,
            standardErrorEncoding,
            cancellationToken,
            CancellationToken.None
        );
    }

    /// <summary>
    /// Executes the command as a pull-based event stream.
    /// </summary>
    /// <remarks>
    /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
    /// </remarks>
    public static IAsyncEnumerable<CommandEvent> ListenAsync(
        this Command command,
        Encoding encoding,
        CancellationToken cancellationToken = default
    )
    {
        return command.ListenAsync(encoding, encoding, cancellationToken);
    }

    /// <summary>
    /// Executes the command as a pull-based event stream.
    /// Uses <see cref="Encoding.Default" /> for decoding.
    /// </summary>
    /// <remarks>
    /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
    /// </remarks>
    public static IAsyncEnumerable<CommandEvent> ListenAsync(
        this Command command,
        CancellationToken cancellationToken = default
    )
    {
        return command.ListenAsync(Encoding.Default, cancellationToken);
    }
}
