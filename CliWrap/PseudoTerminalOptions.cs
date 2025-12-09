namespace CliWrap;

/// <summary>
/// Configuration for pseudo-terminal (PTY) mode.
/// </summary>
/// <remarks>
/// <para>
/// PTY mode makes CLI applications behave as if running in an interactive terminal,
/// enabling colored output, progress indicators, and other TTY-dependent features.
/// </para>
/// <para>
/// Platform support:
/// <list type="bullet">
///   <item>Windows 10 version 1809 (build 17763) or later via ConPTY</item>
///   <item>Linux via forkpty()</item>
///   <item>macOS via forkpty()</item>
/// </list>
/// </para>
/// <para>
/// Note: When PTY is enabled, stderr is merged into stdout on all platforms.
/// The <see cref="Command.StandardErrorPipe" /> will receive an empty stream.
/// </para>
/// </remarks>
public partial class PseudoTerminalOptions(bool isEnabled = false, int columns = 80, int rows = 24)
{
    /// <summary>
    /// Whether PTY mode is enabled.
    /// </summary>
    public bool IsEnabled { get; } = isEnabled;

    /// <summary>
    /// Terminal width in columns.
    /// </summary>
    /// <remarks>
    /// Default is 80 columns.
    /// </remarks>
    public int Columns { get; } = columns;

    /// <summary>
    /// Terminal height in rows.
    /// </summary>
    /// <remarks>
    /// Default is 24 rows.
    /// </remarks>
    public int Rows { get; } = rows;
}

public partial class PseudoTerminalOptions
{
    /// <summary>
    /// Default PTY options (disabled).
    /// </summary>
    public static PseudoTerminalOptions Default { get; } = new();

    /// <summary>
    /// PTY enabled with default terminal size (80x24).
    /// </summary>
    public static PseudoTerminalOptions Enabled { get; } = new(isEnabled: true);
}
