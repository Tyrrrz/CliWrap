using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CliWrap.Builders;

/// <summary>
/// Builder that helps generate well-formed arguments string.
/// </summary>
public partial class ArgumentsBuilder : IArgumentsConfigurator
{
    private static readonly IFormatProvider DefaultFormatProvider = CultureInfo.InvariantCulture;

    private readonly StringBuilder _buffer = new();

    /// <inheritdoc />
    public ArgumentsBuilder Add(string value, bool escape = true)
    {
        if (_buffer.Length > 0)
            _buffer.Append(' ');

        _buffer.Append(escape
            ? Escape(value)
            : value
        );

        return this;
    }

    /// <inheritdoc />
    public ArgumentsBuilder Add(IEnumerable<string> values, bool escape = true)
    {
        foreach (var value in values)
            Add(value, escape);

        return this;
    }

    /// <inheritdoc />
    public ArgumentsBuilder Add(IFormattable value, IFormatProvider formatProvider, bool escape = true) =>
        Add(value.ToString(null, formatProvider), escape);

    /// <inheritdoc />
    public ArgumentsBuilder Add(IFormattable value, bool escape = true) =>
        Add(value, DefaultFormatProvider, escape);

    /// <inheritdoc />
    public ArgumentsBuilder Add(IEnumerable<IFormattable> values, IFormatProvider formatProvider, bool escape = true)
    {
        foreach (var value in values)
            Add(value, formatProvider, escape);

        return this;
    }

    /// <inheritdoc />
    public ArgumentsBuilder Add(IEnumerable<IFormattable> values, bool escape = true) =>
        Add(values, DefaultFormatProvider, escape);

    /// <summary>
    /// Builds the resulting arguments string.
    /// </summary>
    public string Build() => _buffer.ToString();
}

public partial class ArgumentsBuilder
{
    private static string Escape(string argument)
    {
        // Implementation reference:
        // https://github.com/dotnet/runtime/blob/9a50493f9f1125fda5e2212b9d6718bc7cdbc5c0/src/libraries/System.Private.CoreLib/src/System/PasteArguments.cs#L10-L79

        // Short circuit if argument is clean and doesn't need escaping
        if (argument.Length != 0 && argument.All(c => !char.IsWhiteSpace(c) && c != '"'))
            return argument;

        var buffer = new StringBuilder();

        buffer.Append('"');

        for (var i = 0; i < argument.Length;)
        {
            var c = argument[i++];

            if (c == '\\')
            {
                var numBackSlash = 1;
                while (i < argument.Length && argument[i] == '\\')
                {
                    numBackSlash++;
                    i++;
                }

                if (i == argument.Length)
                {
                    buffer.Append('\\', numBackSlash * 2);
                }
                else if (argument[i] == '"')
                {
                    buffer.Append('\\', numBackSlash * 2 + 1);
                    buffer.Append('"');
                    i++;
                }
                else
                {
                    buffer.Append('\\', numBackSlash);
                }
            }
            else if (c == '"')
            {
                buffer.Append('\\');
                buffer.Append('"');
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