using System;
using System.Globalization;

namespace CliWrap.Immersive;

public readonly partial struct CommandLineArgument(string value)
{
    public string Value { get; } = value;

    /// <inheritdoc />
    public override string ToString() => Value;
}

public partial struct CommandLineArgument
{
    public static implicit operator CommandLineArgument(string value) => new(value);

    public static implicit operator string(CommandLineArgument argument) => argument.ToString();

    // Ideally, we'd want to define a single conversion from IFormattable, but
    // unfortunately it's not possible to define a conversion from an interface,
    // so we have to do it for each type individually.

    public static implicit operator CommandLineArgument(bool value) =>
        new(value.ToString(CultureInfo.InvariantCulture));

    public static implicit operator CommandLineArgument(int value) =>
        new(value.ToString(CultureInfo.InvariantCulture));

    public static implicit operator CommandLineArgument(long value) =>
        new(value.ToString(CultureInfo.InvariantCulture));

    public static implicit operator CommandLineArgument(float value) =>
        new(value.ToString(CultureInfo.InvariantCulture));

    public static implicit operator CommandLineArgument(double value) =>
        new(value.ToString(CultureInfo.InvariantCulture));

    public static implicit operator CommandLineArgument(decimal value) =>
        new(value.ToString(CultureInfo.InvariantCulture));

    public static implicit operator CommandLineArgument(char value) =>
        new(value.ToString(CultureInfo.InvariantCulture));

    public static implicit operator CommandLineArgument(byte value) =>
        new(value.ToString(CultureInfo.InvariantCulture));

    public static implicit operator CommandLineArgument(sbyte value) =>
        new(value.ToString(CultureInfo.InvariantCulture));

    public static implicit operator CommandLineArgument(short value) =>
        new(value.ToString(CultureInfo.InvariantCulture));

    public static implicit operator CommandLineArgument(ushort value) =>
        new(value.ToString(CultureInfo.InvariantCulture));

    public static implicit operator CommandLineArgument(uint value) =>
        new(value.ToString(CultureInfo.InvariantCulture));

    public static implicit operator CommandLineArgument(ulong value) =>
        new(value.ToString(CultureInfo.InvariantCulture));

    public static implicit operator CommandLineArgument(Guid value) =>
        new(value.ToString(null, CultureInfo.InvariantCulture));

    public static implicit operator CommandLineArgument(DateTime value) =>
        new(value.ToString(CultureInfo.InvariantCulture));

    public static implicit operator CommandLineArgument(DateTimeOffset value) =>
        new(value.ToString(CultureInfo.InvariantCulture));

    public static implicit operator CommandLineArgument(TimeSpan value) =>
        new(value.ToString(null, CultureInfo.InvariantCulture));
}
