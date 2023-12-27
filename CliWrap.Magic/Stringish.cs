using System;
using System.Globalization;

namespace CliWrap.Magic;

/// <summary>
/// Encapsulates a string value that was implicitly converted from another type.
/// </summary>
public readonly partial struct Stringish
{
    private readonly string _value;

    /// <summary>
    /// Initializes an instance of <see cref="Stringish" />.
    /// </summary>
    public Stringish(string value) => _value = value;

    /// <inheritdoc />
    public override string ToString() => _value;
}

public partial struct Stringish
{
    /// <summary>
    /// Converts a <see cref="string" /> value into <see cref="Stringish" />.
    /// </summary>
    public static implicit operator Stringish(string value) => new(value);

    /// <summary>
    /// Converts a <see cref="bool" /> value into <see cref="Stringish" />.
    /// </summary>
    public static implicit operator Stringish(bool value) => new(value.ToString());

    /// <summary>
    /// Converts a <see cref="IFormattable" /> value into <see cref="Stringish" />.
    /// </summary>
    public static implicit operator Stringish(IFormattable value) =>
        new(value.ToString(null, CultureInfo.InvariantCulture));

    /// <summary>
    /// Converts a <see cref="Stringish" /> value into <see cref="string" />.
    /// </summary>
    public static implicit operator string(Stringish value) => value.ToString();
}
