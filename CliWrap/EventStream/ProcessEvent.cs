using System;

namespace CliWrap.EventStream
{
    /// <summary>
    /// Represents an abstract event produced by a command.
    /// To match an instance of this type with a particular event type use pattern matching or the <code>.OnXyz()</code> extension methods.
    /// </summary>
    public abstract class ProcessEvent { }

    /// <summary>
    /// Represents an event that is triggered when the command starts executing.
    /// This event may only appear once in the event stream.
    /// </summary>
    public class ProcessStartEvent : ProcessEvent
    {
        /// <summary>
        /// Underlying process ID.
        /// </summary>
        public int ProcessId { get; }

        /// <summary>
        /// Initializes an instance of <see cref="ProcessStartEvent"/>.
        /// </summary>
        public ProcessStartEvent(int processId) => ProcessId = processId;
    }

    /// <summary>
    /// Represents an event that is triggered when the underlying process prints a line of text to the standard output stream.
    /// </summary>
    public class StandardOutputLineEvent : ProcessEvent
    {
        /// <summary>
        /// Text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Initializes an instance of <see cref="StandardOutputLineEvent"/>.
        /// </summary>
        public StandardOutputLineEvent(string text) => Text = text;
    }

    /// <summary>
    /// Represents an event that is triggered when the underlying process prints a line of text to the standard error stream.
    /// </summary>
    public class StandardErrorLineEvent : ProcessEvent
    {
        /// <summary>
        /// Text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Initializes an instance of <see cref="StandardErrorLineEvent"/>.
        /// </summary>
        public StandardErrorLineEvent(string text) => Text = text;
    }

    /// <summary>
    /// Represents an event that is triggered when the command finishes executing.
    /// This event may only appear once in the event stream.
    /// </summary>
    public class ProcessExitEvent : ProcessEvent
    {
        /// <summary>
        /// Exit code set by the underlying process.
        /// </summary>
        public int ExitCode { get; }

        /// <summary>
        /// Initializes an instance of <see cref="ProcessExitEvent"/>.
        /// </summary>
        public ProcessExitEvent(int exitCode) => ExitCode = exitCode;
    }

    /// <summary>
    /// Extension methods to simplify pattern matching on <see cref="ProcessEvent"/>.
    /// </summary>
    public static class ProcessEventMatchers
    {
        private static ProcessEvent On<TEvent>(this ProcessEvent processEvent, Action<TEvent> handler) where TEvent : ProcessEvent
        {
            if (processEvent is TEvent matchedEvent)
                handler(matchedEvent);

            return processEvent;
        }

        /// <summary>
        /// Matches the specified event with <see cref="ProcessStartEvent"/>.
        /// </summary>
        public static ProcessEvent OnProcessStart(this ProcessEvent processEvent, Action<ProcessStartEvent> handler) =>
            processEvent.On(handler);

        /// <summary>
        /// Matches the specified event with <see cref="StandardOutputLineEvent"/>.
        /// </summary>
        public static ProcessEvent OnStandardOutputLine(this ProcessEvent processEvent, Action<StandardOutputLineEvent> handler) =>
            processEvent.On(handler);

        /// <summary>
        /// Matches the specified event with <see cref="StandardErrorLineEvent"/>.
        /// </summary>
        public static ProcessEvent OnStandardErrorLine(this ProcessEvent processEvent, Action<StandardErrorLineEvent> handler) =>
            processEvent.On(handler);

        /// <summary>
        /// Matches the specified event with <see cref="ProcessExitEvent"/>.
        /// </summary>
        public static ProcessEvent OnProcessExit(this ProcessEvent processEvent, Action<ProcessExitEvent> handler) =>
            processEvent.On(handler);
    }
}