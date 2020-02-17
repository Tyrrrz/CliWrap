using System.Collections.Generic;
using System.Diagnostics;
using CliWrap.Internal;

namespace CliWrap
{
    public class CliConfiguration
    {
        public string WorkingDirPath { get; }

        public string Arguments { get; }

        public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

        public bool IsExitCodeValidationEnabled { get; }

        public CliConfiguration(string workingDirPath,
            string arguments,
            IReadOnlyDictionary<string, string> environmentVariables,
            bool isExitCodeValidationEnabled)
        {
            WorkingDirPath = workingDirPath;
            Arguments = arguments;
            EnvironmentVariables = environmentVariables;
            IsExitCodeValidationEnabled = isExitCodeValidationEnabled;
        }

        internal ProcessStartInfo GetStartInfo(string filePath) => new ProcessStartInfo
        {
            FileName = filePath,
            Arguments = Arguments,
            WorkingDirectory = WorkingDirPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }.SetEnvironmentVariables(EnvironmentVariables);
    }
}