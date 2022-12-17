using System;
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
    /// <summary>
    /// Executes the command as a push-based event stream.
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
        CancellationToken gracefulCancellationToken)
    {
        return Observable.Create<CommandEvent>(observer =>
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

            // Don't pass cancellation token to the continuation because we need it to
            // trigger regardless of how the task completed.
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
    }

    /// <summary>
    /// Executes the command as a push-based event stream.
    /// </summary>
    /// <remarks>
    /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
    /// </remarks>
    public static IObservable<CommandEvent> Observe(
        this Command command,
        Encoding standardOutputEncoding,
        Encoding standardErrorEncoding,
        CancellationToken cancellationToken = default)
    {
        return command.Observe(
            standardOutputEncoding,
            standardErrorEncoding,
            cancellationToken,
            CancellationToken.None
        );
    }

    /// <summary>
    /// Executes the command as a push-based event stream.
    /// </summary>
    /// <remarks>
    /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
    /// </remarks>
    public static IObservable<CommandEvent> Observe(
        this Command command,
        Encoding encoding,
        CancellationToken cancellationToken = default)
    {
        return command.Observe(
            encoding,
            encoding,
            cancellationToken
        );
    }

    /// <summary>
    /// Executes the command as a push-based event stream.
    /// Uses <see cref="Console.OutputEncoding" /> for decoding.
    /// </summary>
    /// <remarks>
    /// Use pattern matching to handle specific instances of <see cref="CommandEvent" />.
    /// </remarks>
    public static IObservable<CommandEvent> Observe(
        this Command command,
        CancellationToken cancellationToken = default)
    {
        return command.Observe(
            Console.OutputEncoding,
            cancellationToken
        );
    }
}