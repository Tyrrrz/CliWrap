using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Exceptions;
using CliWrap.Internal;

namespace CliWrap
{
    public class BufferedCli
    {
        private readonly string _filePath;
        private readonly CliConfiguration _configuration;
        private readonly Stream _input;

        public BufferedCli(string filePath, CliConfiguration configuration, Stream input)
        {
            _filePath = filePath;
            _configuration = configuration;
            _input = input;
        }

        private async Task<BufferedCliResult> ExecuteAsync(ProcessEx process, CancellationToken cancellationToken = default)
        {
            using var _ = process;

            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();

            process.StdOutReceived += (sender, s) => stdOutBuffer.AppendLine(s);
            process.StdErrReceived += (sender, s) => stdErrBuffer.AppendLine(s);

            process.Start();
            await process.PipeStandardInputAsync(_input, cancellationToken);
            await process.WaitUntilExitAsync(cancellationToken);

            var stdOut = stdOutBuffer.ToString();
            var stdErr = stdErrBuffer.ToString();

            if (_configuration.IsExitCodeValidationEnabled && process.ExitCode != 0)
                throw CliExecutionException.ExitCodeValidation(_filePath, _configuration.Arguments, process.ExitCode, stdErr);

            return new BufferedCliResult(process.ExitCode, process.StartTime, process.ExitTime, stdOut, stdErr);
        }

        public CliTask<BufferedCliResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var process = new ProcessEx(_configuration.GetStartInfo(_filePath));
            var task = ExecuteAsync(process, cancellationToken);

            return new CliTask<BufferedCliResult>(task, process.Id);
        }
    }
}