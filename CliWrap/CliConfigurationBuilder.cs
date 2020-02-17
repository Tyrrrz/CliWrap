using System;
using System.Collections.Generic;
using System.IO;

namespace CliWrap
{
    public class CliConfigurationBuilder
    {
        private string _workingDirPath = Directory.GetCurrentDirectory();
        private string _arguments = "";
        private IReadOnlyDictionary<string, string> _envVars = new Dictionary<string, string>(StringComparer.Ordinal);
        private bool _isExitCodeValidationEnabled = true;

        public CliConfigurationBuilder SetWorkingDirectory(string path)
        {
            _workingDirPath = path;
            return this;
        }

        public CliConfigurationBuilder SetArguments(string arguments)
        {
            _arguments = arguments;
            return this;
        }

        public CliConfigurationBuilder SetArguments(Action<CliArgumentBuilder> configure)
        {
            var builder = new CliArgumentBuilder();
            configure(builder);

            return SetArguments(builder.Build());
        }

        public CliConfigurationBuilder SetEnvironmentVariables(IReadOnlyDictionary<string, string> variables)
        {
            _envVars = variables;
            return this;
        }

        public CliConfigurationBuilder SetEnvironmentVariables(Action<IDictionary<string, string>> configure)
        {
            var variables = new Dictionary<string, string>(StringComparer.Ordinal);
            configure(variables);

            return SetEnvironmentVariables(variables);
        }

        public CliConfigurationBuilder EnableExitCodeValidation(bool isEnabled = true)
        {
            _isExitCodeValidationEnabled = isEnabled;
            return this;
        }

        public CliConfiguration Build() => new CliConfiguration
        (
            _workingDirPath,
            _arguments,
            _envVars,
            _isExitCodeValidationEnabled
        );
    }
}