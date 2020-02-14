using System;
using System.IO;

namespace CliWrap
{
    public class CliConfigurationBuilder
    {
        private string _workingDirPath = Directory.GetCurrentDirectory();
        private string _arguments = "";

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

        public CliConfigurationBuilder SetArguments(Action<CliArgumentFormatter> build)
        {
            var formatter = new CliArgumentFormatter();
            build(formatter);

            return SetArguments(formatter.ToString());
        }

        public CliConfiguration Build() => new CliConfiguration
        (
            _workingDirPath,
            _arguments
        );
    }
}