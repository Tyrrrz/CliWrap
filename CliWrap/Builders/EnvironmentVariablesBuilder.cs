using System;
using System.Collections.Generic;

namespace CliWrap.Builders;

/// <summary>
/// Builder that helps configure environment variables.
/// </summary>
public class EnvironmentVariablesBuilder : IEnvironmentVariablesConfigurator
{
    private readonly IDictionary<string, string?> _envVars = new Dictionary<string, string?>(StringComparer.Ordinal);

    /// <inheritdoc />
    public EnvironmentVariablesBuilder Set(string name, string? value)
    {
        _envVars[name] = value;
        return this;
    }

    /// <inheritdoc />
    public EnvironmentVariablesBuilder Set(IEnumerable<KeyValuePair<string, string?>> variables)
    {
        foreach (var (name, value) in variables)
            Set(name, value);

        return this;
    }

    /// <inheritdoc />
    public EnvironmentVariablesBuilder Set(IReadOnlyDictionary<string, string?> variables) =>
        Set((IEnumerable<KeyValuePair<string, string?>>) variables);

    /// <summary>
    /// Builds the resulting environment variables.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Build() => new Dictionary<string, string?>(_envVars);
}