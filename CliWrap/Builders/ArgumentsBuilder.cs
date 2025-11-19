using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CliWrap.Builders;

/// <summary>
/// Builder that helps format command-line arguments into a string.
/// </summary>
public partial class ArgumentsBuilder
{
    private static readonly IFormatProvider DefaultFormatProvider = CultureInfo.InvariantCulture;

    private readonly StringBuilder _buffer = new();

    /// <summary>
    /// Adds the specified value to the list of arguments.
    /// </summary>
    public ArgumentsBuilder Add(string value, bool escape)
    {
        if (_buffer.Length > 0)
            _buffer.Append(' ');

        _buffer.Append(escape ? Escape(value) : value);

        return this;
    }

    /// <summary>
    /// Adds the specified value to the list of arguments.
    /// </summary>
    // TODO: (breaking change) remove in favor of optional parameter
    public ArgumentsBuilder Add(string value) => Add(value, true);

    /// <summary>
    /// Adds the specified values to the list of arguments.
    /// </summary>
    public ArgumentsBuilder Add(IEnumerable<string> values, bool escape)
    {
        foreach (var value in values)
            Add(value, escape);

        return this;
    }

    /// <summary>
    /// Adds the specified values to the list of arguments.
    /// </summary>
    // TODO: (breaking change) remove in favor of optional parameter
    public ArgumentsBuilder Add(IEnumerable<string> values) => Add(values, true);

    /// <summary>
    /// Adds the specified value to the list of arguments.
    /// </summary>
    public ArgumentsBuilder Add(
        IFormattable value,
        IFormatProvider formatProvider,
        bool escape = true
    ) => Add(value.ToString(null, formatProvider), escape);

    /// <summary>
    /// Adds the specified value to the list of arguments.
    /// </summary>
    // TODO: (breaking change) remove in favor of the other overloads
    public ArgumentsBuilder Add(IFormattable value, CultureInfo cultureInfo, bool escape) =>
        Add(value, (IFormatProvider)cultureInfo, escape);

    /// <summary>
    /// Adds the specified value to the list of arguments.
    /// </summary>
    // TODO: (breaking change) remove in favor of the other overloads
    public ArgumentsBuilder Add(IFormattable value, CultureInfo cultureInfo) =>
        Add(value, cultureInfo, true);

    /// <summary>
    /// Adds the specified value to the list of arguments.
    /// The value is converted to string using invariant culture.
    /// </summary>
    public ArgumentsBuilder Add(IFormattable value, bool escape) =>
        Add(value, DefaultFormatProvider, escape);

    /// <summary>
    /// Adds the specified value to the list of arguments.
    /// The value is converted to string using invariant culture.
    /// </summary>
    // TODO: (breaking change) remove in favor of optional parameter
    public ArgumentsBuilder Add(IFormattable value) => Add(value, true);

    /// <summary>
    /// Adds the specified values to the list of arguments.
    /// </summary>
    public ArgumentsBuilder Add(
        IEnumerable<IFormattable> values,
        IFormatProvider formatProvider,
        bool escape = true
    )
    {
        foreach (var value in values)
            Add(value, formatProvider, escape);

        return this;
    }

    /// <summary>
    /// Adds the specified values to the list of arguments.
    /// </summary>
    // TODO: (breaking change) remove in favor of the other overloads
    public ArgumentsBuilder Add(
        IEnumerable<IFormattable> values,
        CultureInfo cultureInfo,
        bool escape
    ) => Add(values, (IFormatProvider)cultureInfo, escape);

    /// <summary>
    /// Adds the specified values to the list of arguments.
    /// </summary>
    // TODO: (breaking change) remove in favor of the other overloads
    public ArgumentsBuilder Add(IEnumerable<IFormattable> values, CultureInfo cultureInfo) =>
        Add(values, cultureInfo, true);

    /// <summary>
    /// Adds the specified values to the list of arguments.
    /// The values are converted to string using invariant culture.
    /// </summary>
    public ArgumentsBuilder Add(IEnumerable<IFormattable> values, bool escape) =>
        Add(values, DefaultFormatProvider, escape);

    /// <summary>
    /// Adds the specified values to the list of arguments.
    /// The values are converted to string using invariant culture.
    /// </summary>
    // TODO: (breaking change) remove in favor of optional parameter
    public ArgumentsBuilder Add(IEnumerable<IFormattable> values) => Add(values, true);

    /// <summary>
    /// Builds the resulting arguments string.
    /// </summary>
    public string Build() => _buffer.ToString();
}

public partial class ArgumentsBuilder
{
    /// <summary>
    /// Escapes special characters (spaces, slashes, and quotes) in the specified string, ensuring that the output
    /// is correctly interpreted as a single argument when passed to a command-line application.
    /// </summary>
    /// <remarks>
    /// In most cases, you should not need to use this method, as <see cref="ArgumentsBuilder" /> already escapes
    /// arguments automatically. This method is provided for advanced scenarios where you need to escape arguments
    /// manually.
    /// </remarks>
    public static string Escape(string argument)
    {
        // Implementation reference:
        // https://github.com/dotnet/runtime/blob/9a50493f9f1125fda5e2212b9d6718bc7cdbc5c0/src/libraries/System.Private.CoreLib/src/System/PasteArguments.cs#L10-L79
        // MIT License, .NET Foundation

        // Short circuit if the argument is clean and doesn't need escaping
        if (argument.Length > 0 && argument.All(c => !char.IsWhiteSpace(c) && c != '"'))
            return argument;

        var buffer = new StringBuilder();

        buffer.Append('"');

        for (var i = 0; i < argument.Length; )
        {
            var c = argument[i++];

            if (c == '\\')
            {
                var backslashCount = 1;
                while (i < argument.Length && argument[i] == '\\')
                {
                    backslashCount++;
                    i++;
                }

                if (i == argument.Length)
                {
                    buffer.Append('\\', backslashCount * 2);
                }
                else if (argument[i] == '"')
                {
                    buffer.Append('\\', backslashCount * 2 + 1).Append('"');

                    i++;
                }
                else
                {
                    buffer.Append('\\', backslashCount);
                }
            }
            else if (c == '"')
            {
                buffer.Append('\\').Append('"');
            }
            else
            {
                buffer.Append(c);
            }
        }

        buffer.Append('"');

        return buffer.ToString();
    }
}
