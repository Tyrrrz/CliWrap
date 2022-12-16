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
public static class EventStreamCommandExtensions
{
    /// <summary>
    /// Executes the command as an asynchronous (pull-based) event stream.
    /// </summary>
    /// <remarks>
    /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
    /// </remarks>
    // TODO: (breaking change) use optional parameters and remove the other overload
    public static async IAsyncEnumerable<CommandEvent> ListenAsync(
        this Command command,
        Encoding standardOutputEncoding,
        Encoding standardErrorEncoding,
        CancellationToken gracefulCancellationToken,
        [EnumeratorCancellation] CancellationToken forcefulCancellationToken)
    {
        using var channel = new Channel<CommandEvent>();

        var stdOutPipe = PipeTarget.Merge(
            command.StandardOutputPipe,
            PipeTarget.ToDelegate(
                s => channel.PublishAsync(new StandardOutputCommandEvent(s), forcefulCancellationToken),
                standardOutputEncoding
            )
        );

        var stdErrPipe = PipeTarget.Merge(
            command.StandardErrorPipe,
            PipeTarget.ToDelegate(
                s => channel.PublishAsync(new StandardErrorCommandEvent(s), forcefulCancellationToken),
                standardErrorEncoding
            )
        );

        var commandWithPipes = command
            .WithStandardOutputPipe(stdOutPipe)
            .WithStandardErrorPipe(stdErrPipe);

        var commandTask = commandWithPipes.ExecuteAsync(forcefulCancellationToken, gracefulCancellationToken);
        yield return new StartedCommandEvent(commandTask.ProcessId);

        // Don't pass cancellation token to continuation because we need it to always trigger
        // regardless of how the task completed.
        _ = commandTask
            .Task
            .ContinueWith(_ => channel.Close(), TaskContinuationOptions.None);

        await foreach (var cmdEvent in channel.ReceiveAsync(forcefulCancellationToken).ConfigureAwait(false))
            yield return cmdEvent;

        var exitCode = await commandTask.Select(r => r.ExitCode).ConfigureAwait(false);
        yield return new ExitedCommandEvent(exitCode);
    }

    /// <summary>
    /// Executes the command as an asynchronous (pull-based) event stream.
    /// </summary>
    /// <remarks>
    /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
    /// </remarks>
    public static IAsyncEnumerable<CommandEvent> ListenAsync(
        this Command command,
        Encoding standardOutputEncoding,
        Encoding standardErrorEncoding,
        CancellationToken cancellationToken = default) =>
        command.ListenAsync(
            standardOutputEncoding,
            standardErrorEncoding,
            cancellationToken,
            CancellationToken.None
        );

    /// <summary>
    /// Executes the command as an asynchronous (pull-based) event stream.
    /// </summary>
    /// <remarks>
    /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
    /// </remarks>
    public static IAsyncEnumerable<CommandEvent> ListenAsync(
        this Command command,
        Encoding encoding,
        CancellationToken cancellationToken = default) =>
        command.ListenAsync(encoding, encoding, cancellationToken);

    /// <summary>
    /// Executes the command as an asynchronous (pull-based) event stream.
    /// Uses <see cref="Console.OutputEncoding" /> to decode byte streams.
    /// </summary>
    /// <remarks>
    /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
    /// </remarks>
    public static IAsyncEnumerable<CommandEvent> ListenAsync(
        this Command command,
        CancellationToken cancellationToken = default) =>
        command.ListenAsync(Console.OutputEncoding, cancellationToken);

    /// <summary>
    /// Executes the command as an observable (push-based) event stream.
    /// </summary>
    /// <remarks>
    /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
    /// </remarks>
    // TODO: (breaking change) use optional parameters and remove the other overload
    public static IObservable<CommandEvent> Observe(
        this Command command,
        Encoding standardOutputEncoding,
        Encoding standardErrorEncoding,
        CancellationToken forcefulCancellationToken,
        CancellationToken gracefulCancellationToken) =>
        Observable.Create<CommandEvent>(observer =>
        {
            var stdOutPipe = PipeTarget.Merge(
                command.StandardOutputPipe,
                PipeTarget.ToDelegate(
                    s => observer.OnNext(new StandardOutputCommandEvent(s)),
                    standardOutputEncoding
                )
            );

            var stdErrPipe = PipeTarget.Merge(
                command.StandardErrorPipe,
                PipeTarget.ToDelegate(
                    s => observer.OnNext(new StandardErrorCommandEvent(s)),
                    standardErrorEncoding
                )
            );

            var commandWithPipes = command
                .WithStandardOutputPipe(stdOutPipe)
                .WithStandardErrorPipe(stdErrPipe);

            var commandTask = commandWithPipes.ExecuteAsync(forcefulCancellationToken, gracefulCancellationToken);
            observer.OnNext(new StartedCommandEvent(commandTask.ProcessId));

            // Don't pass cancellation token to continuation because we need it to always trigger
            // regardless of how the task completed.
            _ = commandTask
                .Task
                .ContinueWith(t =>
                {
                    // Canceled tasks don't have exceptions
                    if (t.IsCanceled)
                    {
                        observer.OnError(new TaskCanceledException(t));
                    }
                    else if (t.Exception is not null)
                    {
                        observer.OnError(t.Exception.TryGetSingle() ?? t.Exception);
                    }
                    else
                    {
                        observer.OnNext(new ExitedCommandEvent(t.Result.ExitCode));
                        observer.OnCompleted();
                    }
                }, TaskContinuationOptions.None);

            return Disposable.Null;
        });

    /// <summary>
    /// Executes the command as an observable (push-based) event stream.
    /// </summary>
    /// <remarks>
    /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
    /// </remarks>
    public static IObservable<CommandEvent> Observe(
        this Command command,
        Encoding standardOutputEncoding,
        Encoding standardErrorEncoding,
        CancellationToken cancellationToken = default) =>
        command.Observe(
            standardOutputEncoding,
            standardErrorEncoding,
            cancellationToken,
            CancellationToken.None
        );

    /// <summary>
    /// Executes the command as an observable (push-based) event stream.
    /// </summary>
    /// <remarks>
    /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
    /// </remarks>
    public static IObservable<CommandEvent> Observe(
        this Command command,
        Encoding encoding,
        CancellationToken cancellationToken = default) =>
        command.Observe(encoding, encoding, cancellationToken);

    /// <summary>
    /// Executes the command as an observable (push-based) event stream.
    /// Uses <see cref="Console.OutputEncoding" /> to decode byte streams.
    /// </summary>
    /// <remarks>
    /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
    /// </remarks>
    public static IObservable<CommandEvent> Observe(
        this Command command,
        CancellationToken cancellationToken = default) =>
        command.Observe(Console.OutputEncoding, cancellationToken);
}