using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CliWrap.Internal;

namespace CliWrap.EventStream
{
    internal class CommandEventObservable : ICommandEventObservable
    {
        private readonly IList<IObserver<CommandEvent>> _observers = new List<IObserver<CommandEvent>>();

        private readonly Command _command;
        private readonly Encoding _standardOutputEncoding;
        private readonly Encoding _standardErrorEncoding;
        private readonly CancellationToken _cancellationToken;

        public CommandEventObservable(
            Command command,
            Encoding standardOutputEncoding,
            Encoding standardErrorEncoding,
            CancellationToken cancellationToken = default)
        {
            _command = command;
            _standardOutputEncoding = standardOutputEncoding;
            _standardErrorEncoding = standardErrorEncoding;
            _cancellationToken = cancellationToken;
        }

        public void Next(CommandEvent commandEvent)
        {
            foreach (var observer in _observers)
                observer.OnNext(commandEvent);
        }

        public void Completed()
        {
            foreach (var observer in _observers)
                observer.OnCompleted();
        }

        public void Error(Exception exception)
        {
            foreach (var observer in _observers)
                observer.OnError(exception);
        }

        public IDisposable Subscribe(IObserver<CommandEvent> observer)
        {
            _observers.Add(observer);
            return new DelegatedDisposable(() => _observers.Remove(observer));
        }

        public ICommandEventObservable Start()
        {
            // Preserve the existing pipes by merging them with ours
            var stdOutPipe = PipeTarget.Merge(_command.StandardOutputPipe,
                PipeTarget.ToDelegate(s => Next(new StandardOutputCommandEvent(s)), _standardOutputEncoding));
            var stdErrPipe = PipeTarget.Merge(_command.StandardErrorPipe,
                PipeTarget.ToDelegate(s => Next(new StandardErrorCommandEvent(s)), _standardErrorEncoding));

            var commandPiped = _command
                .WithStandardOutputPipe(stdOutPipe)
                .WithStandardErrorPipe(stdErrPipe);

            var commandTask = commandPiped.ExecuteAsync(_cancellationToken);
            Next(new StartedCommandEvent(commandTask.ProcessId));

            _ = commandTask
                .Task
                .ContinueWith(t =>
                {
                    if (t.Exception == null)
                    {
                        Next(new CompletedCommandEvent(t.Result.ExitCode));
                        Completed();
                    }
                    else
                    {
                        Error(t.Exception.Flatten());
                    }
                }, _cancellationToken);

            return this;
        }
    }

    /// <summary>
    /// Observable for command events with delayed initialization.
    /// </summary>
    public interface ICommandEventObservable : IObservable<CommandEvent>
    {
        /// <summary>
        /// Start the execution of the underlying command.
        /// </summary>
        ICommandEventObservable Start();
    }
}