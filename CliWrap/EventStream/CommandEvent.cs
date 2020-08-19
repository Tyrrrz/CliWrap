using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CliWrap.EventStream
{
    /// <summary>
    /// Represents an abstract event produced by a command.
    /// Use pattern matching to handle specific instances of this type.
    /// Can be one of the following:
    /// <see cref="StartedCommandEvent"/>,
    /// <see cref="StandardOutputCommandEvent"/>,
    /// <see cref="StandardErrorCommandEvent"/>,
    /// <see cref="ExitedCommandEvent"/>.
    /// </summary>
    public abstract class CommandEvent { }

    /// <summary>
    /// Represents an event that is triggered when the command starts executing.
    /// This event may only appear once in the event stream.
    /// </summary>
    public class StartedCommandEvent : CommandEvent
    {
        /// <summary>
        /// Underlying process ID.
        /// </summary>
        public int ProcessId { get; }

        /// <summary>
        /// Initializes an instance of <see cref="StartedCommandEvent"/>.
        /// </summary>
        public StartedCommandEvent(int processId) => ProcessId = processId;

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override string ToString() => $"Process ID: {ProcessId}";
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

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override string ToString() => Text;
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

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override string ToString() => Text;
    }

    /// <summary>
    /// Represents an event that is triggered when the command finishes executing.
    /// This event may only appear once in the event stream.
    /// </summary>
    public class ExitedCommandEvent : CommandEvent
    {
        /// <summary>
        /// Exit code set by the underlying process.
        /// </summary>
        public int ExitCode { get; }

        /// <summary>
        /// Initializes an instance of <see cref="ExitedCommandEvent"/>.
        /// </summary>
        public ExitedCommandEvent(int exitCode) => ExitCode = exitCode;

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override string ToString() => $"Exit code: {ExitCode}";
    }
}