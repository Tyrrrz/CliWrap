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

            // Don't pass cancellation token to continuation because we need it to always trigger
            // regardless of how the task completed.
            _ = commandTask
                .Task
                .ContinueWith(_ => channel.Close(), TaskContinuationOptions.None);

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

                // Don't pass cancellation token to continuation because we need it to always trigger
                // regardless of how the task completed.
                _ = commandTask
                    .Task
                    .ContinueWith(t =>
                    {
                        // Canceled tasks don't have exception
                        if (t.IsCanceled)
                        {
                            observer.OnError(new OperationCanceledException("Command execution has been canceled."));
                        }
                        else if (t.Exception == null)
                        {
                            observer.OnNext(new ExitedCommandEvent(t.Result.ExitCode));
                            observer.OnCompleted();
                        }
                        else
                        {
                            observer.OnError(t.Exception);
                        }
                    }, TaskContinuationOptions.None);

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