using System;
using System.Text;
using System.Threading.Tasks;
using CliWrap.Utils;
using CliWrap.Utils.Extensions;

namespace CliWrap.EventStream;

internal class CommandEventObservable : IObservable<CommandEvent>
{
    private readonly Command _command;
    private readonly Encoding _standardOutputEncoding;
    private readonly Encoding _standardErrorEncoding;
    private readonly CommandCancellationToken _cancellationToken;

    public CommandEventObservable(
        Command command,
        Encoding standardOutputEncoding,
        Encoding standardErrorEncoding,
        CommandCancellationToken cancellationToken)
    {
        _command = command;
        _standardOutputEncoding = standardOutputEncoding;
        _standardErrorEncoding = standardErrorEncoding;
        _cancellationToken = cancellationToken;
    }

    public IDisposable Subscribe(IObserver<CommandEvent> observer)
    {
        var stdOutPipe = PipeTarget.Merge(
            _command.StandardOutputPipe,
            PipeTarget.ToDelegate(
                s => observer.OnNext(new StandardOutputCommandEvent(s)),
                _standardOutputEncoding
            )
        );

        var stdErrPipe = PipeTarget.Merge(
            _command.StandardErrorPipe,
            PipeTarget.ToDelegate(
                s => observer.OnNext(new StandardErrorCommandEvent(s)),
                _standardErrorEncoding
            )
        );

        var pipedCommand = _command
            .WithStandardOutputPipe(stdOutPipe)
            .WithStandardErrorPipe(stdErrPipe);

        var commandTask = pipedCommand.ExecuteAsync(_cancellationToken);
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
    }
}