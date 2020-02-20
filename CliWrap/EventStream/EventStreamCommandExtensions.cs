using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Internal;

namespace CliWrap.EventStream
{
    /// <summary>
    /// Convenience extension for executing a command as an event stream.
    /// </summary>
    public static class EventStreamCommandExtensions
    {
        /// <summary>
        /// Executes the command as an asynchronous event stream.
        /// Use <code>await foreach</code> to listen to the stream and handle command events.
        /// </summary>
        public static async IAsyncEnumerable<CommandEvent> ListenAsync(
            this Command command,
            Encoding standardOutputEncoding,
            Encoding standardErrorEncoding,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = new Channel<CommandEvent>();

            // Preserve the existing pipes by merging them with ours
            var stdOutPipe = PipeTarget.Merge(command.StandardOutputPipe,
                PipeTarget.ToDelegate(s => channel.Publish(new StandardOutputCommandEvent(s)), standardOutputEncoding));
            var stdErrPipe = PipeTarget.Merge(command.StandardErrorPipe,
                PipeTarget.ToDelegate(s => channel.Publish(new StandardErrorCommandEvent(s)), standardErrorEncoding));

            var commandPiped = command
                .WithStandardOutputPipe(stdOutPipe)
                .WithStandardErrorPipe(stdErrPipe);

            var commandTask = commandPiped.ExecuteAsync(cancellationToken);

            yield return new StartCommandEvent(commandTask.ProcessId);

            while (!commandTask.Task.IsCompleted && !cancellationToken.IsCancellationRequested)
            {
                if (channel.TryGetNext(out var next))
                    yield return next;
                else
                    await Task.WhenAny(commandTask, channel.WaitUntilNextAsync());
            }

            if (commandTask.Task.IsCompletedSuccessfully())
                yield return new CompleteCommandEvent(commandTask.Task.Result.ExitCode);
        }

        /// <summary>
        /// Executes the command as an asynchronous event stream.
        /// Use <code>await foreach</code> to listen to the stream and handle command events.
        /// </summary>
        public static IAsyncEnumerable<CommandEvent> ListenAsync(
            this Command command,
            Encoding encoding,
            CancellationToken cancellationToken = default) =>
            command.ListenAsync(encoding, encoding, cancellationToken);

        /// <summary>
        /// Executes the command as an asynchronous event stream.
        /// Use <code>await foreach</code> to listen to the stream and handle command events.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the strings from byte streams.
        /// </summary>
        public static IAsyncEnumerable<CommandEvent> ListenAsync(
            this Command command,
            CancellationToken cancellationToken = default) =>
            command.ListenAsync(Console.OutputEncoding, cancellationToken);

        /// <summary>
        /// Executes the command as an observable event stream.
        /// </summary>
        public static ICommandEventObservable Observe(
            this Command command,
            Encoding standardOutputEncoding,
            Encoding standardErrorEncoding,
            CancellationToken cancellationToken = default) =>
            new CommandEventObservable(command, standardOutputEncoding, standardErrorEncoding, cancellationToken);

        /// <summary>
        /// Executes the command as an observable event stream.
        /// </summary>
        public static ICommandEventObservable Observe(
            this Command command,
            Encoding encoding,
            CancellationToken cancellationToken = default) =>
            command.Observe(encoding, encoding, cancellationToken);

        /// <summary>
        /// Executes the command as an observable event stream.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the strings from byte streams.
        /// </summary>
        public static ICommandEventObservable Observe(
            this Command command,
            CancellationToken cancellationToken = default) =>
            command.Observe(Console.OutputEncoding, cancellationToken);
    }
}