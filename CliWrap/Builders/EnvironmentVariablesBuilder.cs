using System;
using System.Collections.Generic;

namespace CliWrap.Builders
{
    /// <summary>
    /// Builder that helps configure environment variables.
    /// </summary>
    public class EnvironmentVariablesBuilder
    {
        private readonly IDictionary<string, string> _envVars = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Sets an environment variable with the specified name to the specified value.
        /// </summary>
        public EnvironmentVariablesBuilder Set(string name, string value)
        {
            _envVars[name] = value;
            return this;
        }

        /// <summary>
        /// Builds the resulting environment variables.
        /// </summary>
        public IReadOnlyDictionary<string, string> Build() => new Dictionary<string, string>(_envVars);
    }
}