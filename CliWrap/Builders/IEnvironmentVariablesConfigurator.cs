using System.Collections.Generic;

namespace CliWrap.Builders;

/// <summary>
/// Methods to configure environment variables.
/// </summary>
public interface IEnvironmentVariablesConfigurator
{
    /// <summary>
    /// Sets an environment variable with the specified name to the specified value.
    /// </summary>
    EnvironmentVariablesBuilder Set(string name, string? value);

    /// <summary>
    /// Sets multiple environment variables from the specified sequence of key-value pairs.
    /// </summary>
    EnvironmentVariablesBuilder Set(IEnumerable<KeyValuePair<string, string?>> variables);

    /// <summary>
    /// Sets multiple environment variables from the specified dictionary.
    /// </summary>
    EnvironmentVariablesBuilder Set(IReadOnlyDictionary<string, string?> variables);
}