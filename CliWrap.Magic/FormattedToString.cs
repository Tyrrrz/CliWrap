using System;
using System.Globalization;

namespace CliWrap.Magic;

public readonly partial struct FormattedToString
{
    public string Value { get; }

    public FormattedToString(string value) => Value = value;
}

public partial struct FormattedToString
{
    public static implicit operator FormattedToString(string value) => new(value);
    
    public static implicit operator FormattedToString(bool value) => new(value.ToString());

    public static implicit operator FormattedToString(IFormattable value) =>
        new(value.ToString(null, CultureInfo.InvariantCulture));
    
    public static implicit operator string(FormattedToString value) => value.Value;
}