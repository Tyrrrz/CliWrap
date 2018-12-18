using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Exceptions;
using CliWrap.Internal;
using CliWrap.Models;

namespace CliWrap
{
    /// <summary>
    /// Command line interface wrapper.
    /// </summary>
    public partial class Cli : ICli
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
        private bool _exitCodeValidation = true;
        private bool _standardErrorValidation;

        /// <summary>
        /// Initializes an instance of <see cref="Cli"/> on the target executable.
        /// </summary>
        public Cli(string filePath)
        {
            _filePath = filePath.GuardNotNull(nameof(filePath));
        }

        #region Options

        /// <inheritdoc />
        public ICli SetWorkingDirectory(string workingDirectory)
        {
            _workingDirectory = workingDirectory.GuardNotNull(nameof(workingDirectory));
            return this;
        }

        /// <inheritdoc />
        public ICli SetArguments(string arguments)
        {
            _arguments = arguments.GuardNotNull(nameof(arguments));
            return this;
        }

        /// <inheritdoc />
        public ICli SetStandardInput(Stream standardInput)
        {
            _standardInput = standardInput.GuardNotNull(nameof(standardInput));
            return this;
        }

        /// <inheritdoc />
        public ICli SetStandardInput(string standardInput, Encoding encoding)
        {
            standardInput.GuardNotNull(nameof(standardInput));
            encoding.GuardNotNull(nameof(encoding));

            // Represent string as stream
            var stream = standardInput.AsStream(encoding);

            return SetStandardInput(stream);
        }

        /// <inheritdoc />
        public ICli SetStandardInput(string standardInput)
        {
            standardInput.GuardNotNull(nameof(standardInput));
            return SetStandardInput(standardInput, Console.InputEncoding);
        }

        /// <inheritdoc />
        public ICli SetEnvironmentVariable(string key, string value)
        {
            key.GuardNotNull(nameof(key));

            // Create dictionary if it's null
            if (_environmentVariables == null)
                _environmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Set variable
            _environmentVariables[key] = value;

            return this;
        }

        /// <inheritdoc />
        public ICli SetStandardOutputEncoding(Encoding encoding)
        {
            _standardOutputEncoding = encoding.GuardNotNull(nameof(encoding));
            return this;
        }

        /// <inheritdoc />
        public ICli SetStandardErrorEncoding(Encoding encoding)
        {
            _standardErrorEncoding = encoding.GuardNotNull(nameof(encoding));
            return this;
        }

        /// <inheritdoc />
        public ICli SetStandardOutputCallback(Action<string> callback)
        {
            _standardOutputObserver = callback.GuardNotNull(nameof(callback));
            return this;
        }

        /// <inheritdoc />
        public ICli SetStandardErrorCallback(Action<string> callback)
        {
            _standardErrorObserver = callback.GuardNotNull(nameof(callback));
            return this;
        }

        /// <inheritdoc />
        public ICli SetCancellationToken(CancellationToken token)
        {
            _cancellationToken = token;
            return this;
        }

        /// <inheritdoc />
        public ICli EnableExitCodeValidation(bool isEnabled = true)
        {
            _exitCodeValidation = isEnabled;
            return this;
        }

        /// <inheritdoc />
        public ICli EnableStandardErrorValidation(bool isEnabled = true)
        {
            _standardErrorValidation = isEnabled;
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
            if (_environmentVariables != null)
                startInfo.SetEnvironmentVariables(_environmentVariables);

            // Create process
            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            return process;
        }

        /// <inheritdoc />
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

                // Record start time
                var startTime = DateTimeOffset.Now;

                try
                {
                    // Begin reading stdout and stderr
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Write stdin
                    using (process.StandardInput)
                        _standardInput?.CopyTo(process.StandardInput.BaseStream);

                    // Wait until exit
                    processMre.Wait(_cancellationToken);

                    // Wait until stdout and stderr finished reading
                    stdOutMre.Wait(_cancellationToken);
                    stdErrMre.Wait(_cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Try to kill process
                    process.TryKill();

                    // Re-throw
                    throw;
                }

                // Record exit time
                var exitTime = DateTimeOffset.Now;

                // Get exit code
                var exitCode = process.ExitCode;

                // Get stdout and stderr
                var stdOut = stdOutBuffer.ToString();
                var stdErr = stdErrBuffer.ToString();

                // Create execution result
                var result = new ExecutionResult(exitCode, stdOut, stdErr, startTime, exitTime);

                // Validate exit code if needed
                if (_exitCodeValidation && result.ExitCode != 0)
                    throw new ExitCodeValidationException(result);

                // Validate standard error if needed
                if (_standardErrorValidation && result.StandardError.IsNotBlank())
                    throw new StandardErrorValidationException(result);

                // Return
                return result;
            }
        }

        /// <inheritdoc />
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

                // Record start time
                var startTime = DateTimeOffset.Now;

                // Begin reading stdout and stderr
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Write stdin
                using (process.StandardInput)
                {
                    if (_standardInput != null)
                        await _standardInput.CopyToAsync(process.StandardInput.BaseStream, 81920, _cancellationToken)
                            .ConfigureAwait(false);
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

                // Wait until stdout and stderr finished reading
                await stdOutTcs.Task.ConfigureAwait(false);
                await stdErrTcs.Task.ConfigureAwait(false);

                // Record exit time
                var exitTime = DateTimeOffset.Now;

                // Get exit code
                var exitCode = process.ExitCode;

                // Get stdout and stderr
                var stdOut = stdOutBuffer.ToString();
                var stdErr = stdErrBuffer.ToString();

                // Create execution result
                var result = new ExecutionResult(exitCode, stdOut, stdErr, startTime, exitTime);

                // Validate exit code if needed
                if (_exitCodeValidation && result.ExitCode != 0)
                    throw new ExitCodeValidationException(result);

                // Validate standard error if needed
                if (_standardErrorValidation && result.StandardError.IsNotBlank())
                    throw new StandardErrorValidationException(result);

                // Return
                return result;
            }
        }

        /// <inheritdoc />
        public void ExecuteAndForget()
        {
            // Create process
            using (var process = CreateProcess())
            {
                // Start process
                process.Start();

                // Write stdin
                using (process.StandardInput)
                    _standardInput?.CopyTo(process.StandardInput.BaseStream);
            }
        }

        #endregion
    }

    public partial class Cli
    {
        /// <summary>
        /// Initializes an instance of <see cref="ICli"/> on the target executable.
        /// </summary>
        public static ICli Wrap(string filePath) => new Cli(filePath);
    }
}