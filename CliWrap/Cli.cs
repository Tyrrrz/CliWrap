using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CliWrap.Infra;

namespace CliWrap
{
    public partial class Cli
    {
        private readonly string _filePath;
        private readonly CliConfiguration _configuration;

        public Cli(string filePath, CliConfiguration configuration)
        {
            _filePath = filePath;
            _configuration = configuration;
        }

        public BufferedCli Buffered() => new BufferedCli(_filePath, _configuration);

        public StreamingCli Streaming() => new StreamingCli(_filePath, _configuration);

        public PipedCli PipeStandardOutput() => new PipedCli();

        public PipedCli PipeStandardError() => new PipedCli();

        private async Task<CliResult> ExecuteAsync(Process process)
        {
            process.OutputDataReceived += (sender, args) => { };
            process.ErrorDataReceived += (sender, args) => { };

            var startTime = DateTimeOffset.Now;
            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var exitCode = await process.WaitForExitAsync();
            var exitTime = DateTimeOffset.Now;

            return new CliResult(exitCode, startTime, exitTime);
        }

        public CliTask ExecuteAsync()
        {
            var process = new Process
            {
                StartInfo = _configuration.GetStartInfo(_filePath)
            };

            return new CliTask(ExecuteAsync(process), process.Id);
        }
    }

    public partial class Cli
    {
        public static Cli Wrap(string filePath, CliConfiguration configuration) =>
            new Cli(filePath, configuration);

        public static Cli Wrap(string filePath, Action<CliConfigurationBuilder> configure)
        {
            var builder = new CliConfigurationBuilder();
            configure(builder);

            return Wrap(filePath, builder.Build());
        }

        public static Cli Wrap(string filePath, string arguments) =>
            Wrap(filePath, configure => configure.SetArguments(arguments));

        public static Cli Wrap(string filePath, string arguments, string workingDirPath) =>
            Wrap(filePath, configure => configure.SetArguments(arguments).SetWorkingDirectory(workingDirPath));

        public static Cli Wrap(string filePath) =>
            Wrap(filePath, CliConfiguration.Default);
    }
}