using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Internal;
using CliWrap.Models;

namespace CliWrap
{
    public class Cli
    {
        private readonly string _filePath;
        private string _workingDirectory;
        private string _arguments;
        private Stream _standardInput;
        private IDictionary<string, string> _environmentVariables;
        private Encoding _standardOutputEncoding = Console.OutputEncoding;
        private Encoding _standardErrorEncoding = Console.OutputEncoding;
        private Action<string> _standardOutputObserver;
        private Action<string> _standardErrorObserver;
        private CancellationToken _cancellationToken;

        public Cli(string filePath)
        {
            _filePath = filePath;
        }

        #region Parameters

        public Cli WithWorkingDirectory(string workingDirectory)
        {
            _workingDirectory = workingDirectory;
            return this;
        }

        public Cli WithArguments(string arguments)
        {
            _arguments = arguments;
            return this;
        }

        public Cli WithStandardInput(Stream standardInput)
        {
            _standardInput = standardInput;
            return this;
        }

        public Cli WithStandardInput(string standardInput, Encoding encoding) =>
            WithStandardInput(standardInput.AsStream(encoding));

        public Cli WithStandardInput(string standardInput) =>
            WithStandardInput(standardInput, Console.InputEncoding);

        public Cli WithEnvironmentVariable(string key, string value)
        {
            if (_environmentVariables == null)
                _environmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            _environmentVariables[key] = value;

            return this;
        }

        public Cli WithStandardOutputEncoding(Encoding standardOutputEncoding)
        {
            _standardOutputEncoding = standardOutputEncoding;
            return this;
        }

        public Cli WithStandardErrorEncoding(Encoding standardErrorEncoding)
        {
            _standardErrorEncoding = standardErrorEncoding;
            return this;
        }

        public Cli WithStandardOutputObserver(Action<string> observer)
        {
            _standardOutputObserver = observer;
            return this;
        }

        public Cli WithStandardOutputObserver(IObserver<string> observer) =>
            WithStandardOutputObserver(observer.OnNext);

        public Cli WithStandardErrorObserver(Action<string> observer)
        {
            _standardErrorObserver = observer;
            return this;
        }

        public Cli WithStandardErrorObserver(IObserver<string> observer) =>
            WithStandardOutputObserver(observer.OnNext);

        public Cli WithCancellationToken(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            return this;
        }

        #endregion

        #region Execute

        private Process CreateProcess()
        {
            // Create process start info
            var startInfo = new ProcessStartInfo
            {
                FileName = _filePath,
                WorkingDirectory = _workingDirectory,
                Arguments = _arguments,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                StandardOutputEncoding = _standardOutputEncoding,
                StandardErrorEncoding = _standardErrorEncoding,
                UseShellExecute = false
            };

            // Set environment variables
            startInfo.SetEnvironmentVariables(_environmentVariables);

            // Create process
            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            return process;
        }

        public ExecutionResult Execute()
        {
            // Set up execution context
            using (var processMre = new ManualResetEventSlim())
            using (var stdOutMre = new ManualResetEventSlim())
            using (var stdErrMre = new ManualResetEventSlim())
            using (var process = CreateProcess())
            {
                // Create buffers
                var stdOutBuffer = new StringBuilder();
                var stdErrBuffer = new StringBuilder();

                // Wire events
                process.Exited += (sender, args) => processMre.Set();
                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        stdOutBuffer.AppendLine(args.Data);
                        _standardOutputObserver?.Invoke(args.Data);
                    }
                    else
                    {
                        stdOutMre.Set();
                    }
                };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        stdErrBuffer.AppendLine(args.Data);
                        _standardErrorObserver?.Invoke(args.Data);
                    }
                    else
                    {
                        stdErrMre.Set();
                    }
                };

                // Start process
                process.Start();
                var startTime = DateTimeOffset.Now;

                // Begin reading stdout and stderr
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Write stdin
                using (process.StandardInput)
                {
                    _standardInput?.CopyTo(process.StandardInput.BaseStream);
                }

                // Setup cancellation token to kill process and set events
                // This has to be after process start so that it can actually be killed
                // and also after standard input so that it can write correctly
                _cancellationToken.Register(() =>
                {
                    process.TryKill();
                    processMre.Set();
                    stdOutMre.Set();
                    stdErrMre.Set();
                });

                // Cancellation token is not passed to waits because
                // the callback has to finish executing before the process is disposed
                // which otherwise would happen too soon

                // Wait until exit
                processMre.Wait();
                var exitTime = DateTimeOffset.Now;

                // Wait until stdout and stderr finished reading
                stdOutMre.Wait();
                stdErrMre.Wait();

                // Check cancellation
                _cancellationToken.ThrowIfCancellationRequested();

                // Get stdout and stderr
                var stdOut = stdOutBuffer.ToString();
                var stdErr = stdErrBuffer.ToString();

                return new ExecutionResult(process.ExitCode, stdOut, stdErr, startTime, exitTime);
            }
        }

        public void ExecuteAndForget()
        {
            // Create process
            using (var process = CreateProcess())
            {
                // Start process
                process.Start();

                // Write stdin
                using (process.StandardInput)
                {
                    _standardInput?.CopyTo(process.StandardInput.BaseStream);
                }
            }
        }

        public async Task<ExecutionResult> ExecuteAsync()
        {
            // Create task completion sources
            var processTcs = new TaskCompletionSource<object>();
            var stdOutTcs = new TaskCompletionSource<object>();
            var stdErrTcs = new TaskCompletionSource<object>();

            // Set up execution context
            using (var process = CreateProcess())
            {
                // Create buffers
                var stdOutBuffer = new StringBuilder();
                var stdErrBuffer = new StringBuilder();

                // Wire events
                process.Exited += (sender, args) => processTcs.TrySetResult(null);
                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        stdOutBuffer.AppendLine(args.Data);
                        _standardOutputObserver?.Invoke(args.Data);
                    }
                    else
                    {
                        stdOutTcs.TrySetResult(null);
                    }
                };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        stdErrBuffer.AppendLine(args.Data);
                        _standardErrorObserver?.Invoke(args.Data);
                    }
                    else
                    {
                        stdErrTcs.TrySetResult(null);
                    }
                };

                // Start process
                process.Start();
                var startTime = DateTimeOffset.Now;

                // Begin reading stdout and stderr
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Write stdin
                using (process.StandardInput)
                {
                    if (_standardInput != null)
                        await _standardInput.CopyToAsync(process.StandardInput.BaseStream).ConfigureAwait(false);
                }

                // Setup cancellation token to kill process and cancel tasks
                // This has to be after process start so that it can actually be killed
                // and also after standard input so that it can write correctly
                _cancellationToken.Register(() =>
                {
                    process.TryKill();
                    processTcs.TrySetCanceled();
                    stdOutTcs.TrySetCanceled();
                    stdErrTcs.TrySetCanceled();
                });

                // Wait until exit
                await processTcs.Task.ConfigureAwait(false);
                var exitTime = DateTimeOffset.Now;

                // Wait until stdout and stderr finished reading
                await stdOutTcs.Task.ConfigureAwait(false);
                await stdErrTcs.Task.ConfigureAwait(false);

                // Get stdout and stderr
                var stdOut = stdOutBuffer.ToString();
                var stdErr = stdErrBuffer.ToString();

                return new ExecutionResult(process.ExitCode, stdOut, stdErr, startTime, exitTime);
            }
        }

        #endregion
    }
}