using System;

namespace CliWrap.EventStream
{
    /// <summary>
    /// Represents an abstract event produced by a command.
    /// To match an instance of this type with a particular event type use pattern matching or the <code>.OnXyz()</code> extension methods.
    /// </summary>
    public abstract class CommandEvent { }

    /// <summary>
    /// Represents an event that is triggered when the command starts executing.
    /// This event may only appear once in the event stream.
    /// </summary>
    public class StartCommandEvent : CommandEvent
    {
        /// <summary>
        /// Underlying process ID.
        /// </summary>
        public int ProcessId { get; }

        /// <summary>
        /// Initializes an instance of <see cref="StartCommandEvent"/>.
        /// </summary>
        public StartCommandEvent(int processId) => ProcessId = processId;
    }

    /// <summary>
    /// Represents an event that is triggered when the underlying process prints a line of text to the standard output stream.
    /// </summary>
    public class StandardOutputCommandEvent : CommandEvent
    {
        /// <summary>
        /// Text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Initializes an instance of <see cref="StandardOutputCommandEvent"/>.
        /// </summary>
        public StandardOutputCommandEvent(string text) => Text = text;
    }

    /// <summary>
    /// Represents an event that is triggered when the underlying process prints a line of text to the standard error stream.
    /// </summary>
    public class StandardErrorCommandEvent : CommandEvent
    {
        /// <summary>
        /// Text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Initializes an instance of <see cref="StandardErrorCommandEvent"/>.
        /// </summary>
        public StandardErrorCommandEvent(string text) => Text = text;
    }

    /// <summary>
    /// Represents an event that is triggered when the command finishes executing.
    /// This event may only appear once in the event stream.
    /// </summary>
    public class CompleteCommandEvent : CommandEvent
    {
        /// <summary>
        /// Exit code set by the underlying process.
        /// </summary>
        public int ExitCode { get; }

        /// <summary>
        /// Initializes an instance of <see cref="CompleteCommandEvent"/>.
        /// </summary>
        public CompleteCommandEvent(int exitCode) => ExitCode = exitCode;
    }

    /// <summary>
    /// Extension methods to simplify pattern matching on <see cref="CommandEvent"/>.
    /// </summary>
    public static class ProcessEventMatchers
    {
        private static CommandEvent On<TEvent>(this CommandEvent commandEvent, Action<TEvent> handler) where TEvent : CommandEvent
        {
            if (commandEvent is TEvent matchedEvent)
                handler(matchedEvent);

            return commandEvent;
        }

        /// <summary>
        /// Matches the specified event with <see cref="StartCommandEvent"/>.
        /// </summary>
        public static CommandEvent OnStart(this CommandEvent commandEvent, Action<StartCommandEvent> handler) =>
            commandEvent.On(handler);

        /// <summary>
        /// Matches the specified event with <see cref="StandardOutputCommandEvent"/>.
        /// </summary>
        public static CommandEvent OnStandardOutput(this CommandEvent commandEvent, Action<StandardOutputCommandEvent> handler) =>
            commandEvent.On(handler);

        /// <summary>
        /// Matches the specified event with <see cref="StandardErrorCommandEvent"/>.
        /// </summary>
        public static CommandEvent OnStandardError(this CommandEvent commandEvent, Action<StandardErrorCommandEvent> handler) =>
            commandEvent.On(handler);

        /// <summary>
        /// Matches the specified event with <see cref="CompleteCommandEvent"/>.
        /// </summary>
        public static CommandEvent OnComplete(this CommandEvent commandEvent, Action<CompleteCommandEvent> handler) =>
            commandEvent.On(handler);
    }
}