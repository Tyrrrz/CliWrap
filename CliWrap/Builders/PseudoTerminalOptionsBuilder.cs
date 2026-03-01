namespace CliWrap.Builders;

/// <summary>
/// Builder that helps configure pseudo-terminal options.
/// </summary>
public class PseudoTerminalOptionsBuilder
{
    private bool _isEnabled = true;
    private int _columns = 80;
    private int _rows = 24;

    /// <summary>
    /// Sets whether PTY mode is enabled.
    /// </summary>
    /// <remarks>
    /// Default is <c>true</c> when using the builder.
    /// </remarks>
    public PseudoTerminalOptionsBuilder SetEnabled(bool enabled = true)
    {
        _isEnabled = enabled;
        return this;
    }

    /// <summary>
    /// Sets the terminal dimensions.
    /// </summary>
    /// <param name="columns">Terminal width in columns. Default is 80.</param>
    /// <param name="rows">Terminal height in rows. Default is 24.</param>
    public PseudoTerminalOptionsBuilder SetSize(int columns, int rows)
    {
        _columns = columns;
        _rows = rows;
        return this;
    }

    /// <summary>
    /// Sets the terminal width in columns.
    /// </summary>
    /// <remarks>
    /// Default is 80 columns.
    /// </remarks>
    public PseudoTerminalOptionsBuilder SetColumns(int columns)
    {
        _columns = columns;
        return this;
    }

    /// <summary>
    /// Sets the terminal height in rows.
    /// </summary>
    /// <remarks>
    /// Default is 24 rows.
    /// </remarks>
    public PseudoTerminalOptionsBuilder SetRows(int rows)
    {
        _rows = rows;
        return this;
    }

    /// <summary>
    /// Builds the resulting PTY options.
    /// </summary>
    public PseudoTerminalOptions Build() => new(_isEnabled, _columns, _rows);
}
