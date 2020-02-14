using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CliWrap.Infra;

namespace CliWrap
{
    public partial class Cli
    {
        private readonly string _filePath;
        private readonly CliConfiguration _configuration;
        private readonly Stream _input;

        public Cli(string filePath, CliConfiguration configuration, Stream input)
        {
            _filePath = filePath;
            _configuration = configuration;
            _input = input;
        }

        public Cli(string filePath, CliConfiguration configuration)
            : this(filePath, configuration, Stream.Null)
        {
        }

        public Cli PipeFrom(Stream input) => new Cli(_filePath, _configuration, input);

        public Cli PipeFrom(byte[] data) => PipeFrom(data.ToStream());

        public Cli PipeFrom(string input, Encoding encoding) => PipeFrom(encoding.GetBytes(input));

        public Cli PipeFrom(string input) => PipeFrom(input, Console.InputEncoding);

        public Cli PipeFrom(Cli cli)
        {
            var process = new Process
            {
                StartInfo = cli._configuration.GetStartInfo(cli._filePath)
            };

            return PipeFrom(new ProcessStream(process));
        }

        public BufferedCli Buffered() => new BufferedCli(_filePath, _configuration, _input);

        public StreamingCli Streaming() => new StreamingCli(_filePath, _configuration);

        private async Task<CliResult> ExecuteAsync(Process process)
        {
            process.OutputDataReceived += (sender, args) => { };
            process.ErrorDataReceived += (sender, args) => { };

            var startTime = DateTimeOffset.Now;
            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using (process.StandardInput)
                await _input.CopyToAsync(process.StandardInput.BaseStream);

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
        public static Cli operator >(Cli source, Cli target) => target.PipeFrom(source);

        public static Cli operator <(Cli target, Cli source) => target.PipeFrom(source);
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
            Wrap(filePath, c => c.SetArguments(arguments));

        public static Cli Wrap(string filePath, string arguments, string workingDirPath) =>
            Wrap(filePath, c => c.SetArguments(arguments).SetWorkingDirectory(workingDirPath));

        public static Cli Wrap(string filePath) =>
            Wrap(filePath, CliConfiguration.Default);
    }
}