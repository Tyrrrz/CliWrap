using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Infra;

namespace CliWrap
{
    public class BufferedCli
    {
        private readonly string _filePath;
        private readonly CliConfiguration _configuration;

        public BufferedCli(string filePath, CliConfiguration configuration)
        {
            _filePath = filePath;
            _configuration = configuration;
        }

        private async Task<BufferedCliResult> ExecuteAsync(Process process)
        {
            using var stdOutEndSignal = new SemaphoreSlim(0);
            using var stdErrEndSignal = new SemaphoreSlim(0);

            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();

            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                    stdOut.AppendLine(args.Data);
                else
                    stdOutEndSignal.Release();
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                    stdErr.AppendLine(args.Data);
                else
                    stdErrEndSignal.Release();
            };

            var startTime = DateTimeOffset.Now;
            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await stdOutEndSignal.WaitAsync();
            await stdErrEndSignal.WaitAsync();
            var exitCode = await process.WaitForExitAsync();
            var exitTime = DateTimeOffset.Now;

            return new BufferedCliResult(exitCode, startTime, exitTime, stdOut.ToString(), stdErr.ToString());
        }

        public BufferedCliTask ExecuteAsync()
        {
            var process = new Process
            {
                StartInfo = _configuration.GetStartInfo(_filePath)
            };

            return new BufferedCliTask(ExecuteAsync(process), process.Id);
        }
    }
}