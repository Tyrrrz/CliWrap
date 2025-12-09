using System;

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
///   <item>Linux via openpty/posix_spawn with full PTY I/O</item>
///   <item>macOS via openpty/posix_spawn with full PTY I/O</item>
/// </list>
/// </para>
/// <para>
/// Note: When PTY is enabled, stderr is merged into stdout on all platforms.
/// The <see cref="Command.StandardErrorPipe" /> will receive an empty stream.
/// </para>
/// </remarks>
public partial class PseudoTerminalOptions
{
    /// <summary>
    /// Creates a new instance of <see cref="PseudoTerminalOptions"/>.
    /// </summary>
    /// <param name="isEnabled">Whether PTY mode is enabled.</param>
    /// <param name="columns">Terminal width in columns (must be positive).</param>
    /// <param name="rows">Terminal height in rows (must be positive).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when columns or rows is less than or equal to zero.
    /// </exception>
    public PseudoTerminalOptions(bool isEnabled = false, int columns = 80, int rows = 24)
    {
        if (columns <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(columns),
                columns,
                "Terminal columns must be positive."
            );

        if (rows <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(rows),
                rows,
                "Terminal rows must be positive."
            );

        IsEnabled = isEnabled;
        Columns = columns;
        Rows = rows;
    }

    /// <summary>
    /// Whether PTY mode is enabled.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Terminal width in columns.
    /// </summary>
    /// <remarks>
    /// Default is 80 columns.
    /// </remarks>
    public int Columns { get; }

    /// <summary>
    /// Terminal height in rows.
    /// </summary>
    /// <remarks>
    /// Default is 24 rows.
    /// </remarks>
    public int Rows { get; }
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
