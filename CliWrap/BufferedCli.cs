using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CliWrap.Exceptions;

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
            using (process)
            {
                var processExitTcs = new TaskCompletionSource<object?>();
                var stdOutEndTcs = new TaskCompletionSource<object?>();
                var stdErrEndTcs = new TaskCompletionSource<object?>();

                var stdOutBuffer = new StringBuilder();
                var stdErrBuffer = new StringBuilder();

                process.EnableRaisingEvents = true;
                process.Exited += (sender, args) => processExitTcs.TrySetResult(null);

                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                        stdOutBuffer.AppendLine(args.Data);
                    else
                        stdOutEndTcs.TrySetResult(null);
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                        stdErrBuffer.AppendLine(args.Data);
                    else
                        stdErrEndTcs.TrySetResult(null);
                };

                var startTime = DateTimeOffset.Now;
                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using (process.StandardInput)
                    await _input.CopyToAsync(process.StandardInput.BaseStream);

                await processExitTcs.Task;
                await stdOutEndTcs.Task;
                await stdErrEndTcs.Task;

                var exitCode = process.ExitCode;
                var exitTime = DateTimeOffset.Now;

                var stdOut = stdOutBuffer.ToString();
                var stdErr = stdErrBuffer.ToString();

                if (_configuration.IsExitCodeValidationEnabled && exitCode != 0)
                    throw CliExecutionException.ExitCodeValidation(_filePath, _configuration.Arguments, exitCode, stdErr);

                return new BufferedCliResult(exitCode, startTime, exitTime, stdOut, stdErr);
            }
        }

        public CliTask<BufferedCliResult> ExecuteAsync()
        {
            var process = new Process
            {
                StartInfo = _configuration.GetStartInfo(_filePath)
            };

            return new CliTask<BufferedCliResult>(ExecuteAsync(process), process.Id);
        }
    }
}