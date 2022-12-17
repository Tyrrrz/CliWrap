using System.Diagnostics.CodeAnalysis;

namespace CliWrap.EventStream;

/// <summary>
/// <para>
///     Abstract event produced by a command.
///     Use pattern matching to handle specific instances of this type.
/// </para>
/// <para>
///     Can be either one of the following:
///     <list type="bullet">
///         <item><see cref="StartedCommandEvent" /></item>
///         <item><see cref="StandardOutputCommandEvent" /></item>
///         <item><see cref="StandardErrorCommandEvent" /></item>
///         <item><see cref="ExitedCommandEvent" /></item>
///     </list>
/// </para>
/// </summary>
public abstract class CommandEvent
{
}

/// <summary>
/// Event triggered when the command starts executing.
/// May only appear once in the event stream.
/// </summary>
public class StartedCommandEvent : CommandEvent
{
    /// <summary>
    /// Underlying process ID.
    /// </summary>
    public int ProcessId { get; }

    /// <summary>
    /// Initializes an instance of <see cref="StartedCommandEvent" />.
    /// </summary>
    public StartedCommandEvent(int processId) => ProcessId = processId;

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public override string ToString() => $"Process ID: {ProcessId}";
}

/// <summary>
/// Event triggered when the underlying process writes a line of text to the standard output stream.
/// </summary>
public class StandardOutputCommandEvent : CommandEvent
{
    /// <summary>
    /// Line of text written to the standard output stream.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Initializes an instance of <see cref="StandardOutputCommandEvent" />.
    /// </summary>
    public StandardOutputCommandEvent(string text) => Text = text;

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public override string ToString() => Text;
}

/// <summary>
/// Event triggered when the underlying process writes a line of text to the standard error stream.
/// </summary>
public class StandardErrorCommandEvent : CommandEvent
{
    /// <summary>
    /// Line of text written to the standard error stream.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Initializes an instance of <see cref="StandardErrorCommandEvent" />.
    /// </summary>
    public StandardErrorCommandEvent(string text) => Text = text;

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public override string ToString() => Text;
}

/// <summary>
/// Event triggered when the command finishes executing.
/// May only appear once in the event stream.
/// </summary>
public class ExitedCommandEvent : CommandEvent
{
    /// <summary>
    /// Exit code set by the underlying process.
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    /// Initializes an instance of <see cref="ExitedCommandEvent" />.
    /// </summary>
    public ExitedCommandEvent(int exitCode) => ExitCode = exitCode;

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public override string ToString() => $"Exit code: {ExitCode}";
}