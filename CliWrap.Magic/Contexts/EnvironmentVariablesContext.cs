using System.Collections.Generic;
using Contextual;

namespace CliWrap.Magic.Contexts;

internal class EnvironmentVariablesContext : Context
{
    public IReadOnlyDictionary<string, string?> Variables { get; }

    public EnvironmentVariablesContext(IReadOnlyDictionary<string, string?> variables) =>
        Variables = variables;

    public EnvironmentVariablesContext()
        : this(new Dictionary<string, string?>()) { }
}
