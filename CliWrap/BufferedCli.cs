using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

            using (process.StandardInput)
            {
                await _input.CopyToAsync(process.StandardInput.BaseStream);
            }

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