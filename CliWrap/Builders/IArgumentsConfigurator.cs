using System;
using System.Collections.Generic;

namespace CliWrap.Builders;

/// <summary>
/// Methods to configure arguments.
/// </summary>
public interface IArgumentsConfigurator
{
    /// <summary>
    /// Adds the specified value to the list of arguments.
    /// </summary>
    ArgumentsBuilder Add(string value, bool escape = true);

    /// <summary>
    /// Adds the specified values to the list of arguments.
    /// </summary>
    ArgumentsBuilder Add(IEnumerable<string> values, bool escape = true);

    /// <summary>
    /// Adds the specified value to the list of arguments.
    /// </summary>
    ArgumentsBuilder Add(IFormattable value, IFormatProvider formatProvider, bool escape = true);

    /// <summary>
    /// Adds the specified value to the list of arguments.
    /// The value is converted to string using invariant culture.
    /// </summary>
    ArgumentsBuilder Add(IFormattable value, bool escape = true);

    /// <summary>
    /// Adds the specified values to the list of arguments.
    /// </summary>
    ArgumentsBuilder Add(IEnumerable<IFormattable> values, IFormatProvider formatProvider, bool escape = true);

    /// <summary>
    /// Adds the specified values to the list of arguments.
    /// The values are converted to string using invariant culture.
    /// </summary>
    ArgumentsBuilder Add(IEnumerable<IFormattable> values, bool escape = true);
}