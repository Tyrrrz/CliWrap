using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
            using var channel = new Channel<CommandEvent>();

            var stdOutPipe = PipeTarget.Merge(command.StandardOutputPipe,
                PipeTarget.ToDelegate(
                    s => channel.PublishAsync(new StandardOutputCommandEvent(s), cancellationToken),
                    standardOutputEncoding));

            var stdErrPipe = PipeTarget.Merge(command.StandardErrorPipe,
                PipeTarget.ToDelegate(
                    s => channel.PublishAsync(new StandardErrorCommandEvent(s), cancellationToken),
                    standardErrorEncoding));

            var commandPiped = command
                .WithStandardOutputPipe(stdOutPipe)
                .WithStandardErrorPipe(stdErrPipe);

            var commandTask = commandPiped.ExecuteAsync(cancellationToken);
            yield return new StartedCommandEvent(commandTask.ProcessId);

            _ = commandTask
                .Task
                .ContinueWith(_ => channel.Close(), cancellationToken);

            await foreach (var cmdEvent in channel.ReceiveAsync(cancellationToken))
            {
                yield return cmdEvent;
            }

            var exitCode = await commandTask.Select(r => r.ExitCode);
            yield return new ExitedCommandEvent(exitCode);
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
        public static IObservable<CommandEvent> Observe(
            this Command command,
            Encoding standardOutputEncoding,
            Encoding standardErrorEncoding,
            CancellationToken cancellationToken = default) =>
            Observable.Create<CommandEvent>(observer =>
            {
                var stdOutPipe = PipeTarget.Merge(command.StandardOutputPipe,
                    PipeTarget.ToDelegate(
                        s => observer.OnNext(new StandardOutputCommandEvent(s)),
                        standardOutputEncoding));

                var stdErrPipe = PipeTarget.Merge(command.StandardErrorPipe,
                    PipeTarget.ToDelegate(
                        s => observer.OnNext(new StandardErrorCommandEvent(s)),
                        standardErrorEncoding));

                var commandPiped = command
                    .WithStandardOutputPipe(stdOutPipe)
                    .WithStandardErrorPipe(stdErrPipe);

                var commandTask = commandPiped.ExecuteAsync(cancellationToken);
                observer.OnNext(new StartedCommandEvent(commandTask.ProcessId));

                _ = commandTask
                    .Task
                    .ContinueWith(t =>
                    {
                        if (t.Exception == null)
                        {
                            observer.OnNext(new ExitedCommandEvent(t.Result.ExitCode));
                            observer.OnCompleted();
                        }
                        else
                        {
                            observer.OnError(t.Exception);
                        }
                    }, cancellationToken);

                return Disposable.Null;
            });

        /// <summary>
        /// Executes the command as an observable event stream.
        /// </summary>
        public static IObservable<CommandEvent> Observe(
            this Command command,
            Encoding encoding,
            CancellationToken cancellationToken = default) =>
            command.Observe(encoding, encoding, cancellationToken);

        /// <summary>
        /// Executes the command as an observable event stream.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the strings from byte streams.
        /// </summary>
        public static IObservable<CommandEvent> Observe(
            this Command command,
            CancellationToken cancellationToken = default) =>
            command.Observe(Console.OutputEncoding, cancellationToken);
    }
}