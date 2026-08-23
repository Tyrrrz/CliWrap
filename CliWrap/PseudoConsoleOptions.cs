namespace CliWrap;

/// <summary>
/// Options for running a process under a pseudo-terminal (PTY).
/// </summary>
/// <remarks>
/// <para>
/// When a pseudo-terminal is enabled, the underlying process believes it is running in an
/// interactive terminal rather than a scripting environment.  This causes many CLI tools to
/// produce colored output, progress bars, and other TTY-dependent behavior that they would
/// otherwise suppress when they detect that their streams are redirected.
/// </para>
/// <para>
/// Because a PTY merges the standard error stream into the standard output stream at the
/// operating system level, <see cref="Command.StandardErrorPipe" /> will always receive an
/// empty stream when this option is active.  Configuring a non-null stderr pipe alongside
/// pseudo-terminal mode is therefore a no-op.
/// </para>
/// <para>
/// Platform support:
/// <list type="bullet">
///   <item>Linux and macOS — always supported.</item>
///   <item>Windows — requires Windows 10 version 1809 (build 17763) or later (ConPTY).</item>
/// </list>
/// </para>
/// </remarks>
public class PseudoConsoleOptions(int columns = 80, int rows = 24)
{
    /// <summary>
    /// Width of the pseudo-terminal in character columns.
    /// </summary>
    public int Columns { get; } = columns;

    /// <summary>
    /// Height of the pseudo-terminal in character rows.
    /// </summary>
    public int Rows { get; } = rows;

    /// <summary>
    /// Default pseudo-console options (80 columns × 24 rows).
    /// </summary>
    public static PseudoConsoleOptions Default { get; } = new();
}
