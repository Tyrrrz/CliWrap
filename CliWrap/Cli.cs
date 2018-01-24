using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Internal;
using CliWrap.Models;
using CliWrap.Services;

namespace CliWrap
{
    /// <summary>
    /// Wrapper for a command line interface.
    /// </summary>
    public class Cli : IDisposable
    {
        private readonly object _lock = new object();

        private CancellationTokenSource _killSwitchCts;

        /// <summary>
        /// File path of the target executable.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Settings to use when executing the target executable.
        /// </summary>
        public CliSettings Settings { get; }

        /// <summary>
        /// Initializes wrapper on a target executable with given settings.
        /// </summary>
        /// <param name="filePath">File path of the target executable.</param>
        /// <param name="settings">Settings to use when executing the target executable.</param>
        public Cli(string filePath, CliSettings settings)
        {
            FilePath = filePath.GuardNotNull(nameof(filePath));
            Settings = settings.GuardNotNull(nameof(settings));

            // Freeze settings to prevent changes
            Settings.Freeze();

            // Create kill switch
            _killSwitchCts = new CancellationTokenSource();
        }

        /// <summary>
        /// Initializes wrapper on a target executable with default settings.
        /// </summary>
        /// <param name="filePath">File path of the target executable.</param>
        public Cli(string filePath)
            : this(filePath, CliSettings.Default)
        {
        }

        private Process CreateProcess(ExecutionInput input)
        {
            // Create process
            var process = new Process
            {
                StartInfo =
                {
                    FileName = FilePath,
                    WorkingDirectory = Settings.WorkingDirectory,
                    Arguments = input.Arguments,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    StandardOutputEncoding = Settings.Encoding.StandardOutput,
                    StandardErrorEncoding = Settings.Encoding.StandardError,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            // Set environment variables
            process.StartInfo.SetEnvironmentVariables(input.EnvironmentVariables);

            return process;
        }

        #region Execute

        /// <summary>
        /// Executes target process with given input, waits until completion synchronously and returns produced output.
        /// </summary>
        /// <param name="input">Execution input.</param>
        /// <param name="cancellationToken">Token that can be used to abort execution.</param>
        /// <param name="bufferHandler">Handler for real-time standard output and standard error data.</param>
        /// <remarks>The underlying process is killed if the execution is canceled.</remarks>
        public ExecutionOutput Execute(ExecutionInput input,
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null)
        {
            input.GuardNotNull(nameof(input));

            // Set up execution context
            using (var processMre = new ManualResetEventSlim())
            using (var stdOutMre = new ManualResetEventSlim())
            using (var stdErrMre = new ManualResetEventSlim())
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _killSwitchCts.Token))
            using (var process = CreateProcess(input))
            {
                // Get linked cancellation token
                var linkedToken = linkedCts.Token;

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
                        bufferHandler?.HandleStandardOutput(args.Data);
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
                        bufferHandler?.HandleStandardError(args.Data);
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
                    if (input.StandardInput != null)
                    {
                        var stdinData = Settings.Encoding.StandardInput.GetBytes(input.StandardInput);
                        var stdinStream = process.StandardInput.BaseStream;
                        stdinStream.Write(stdinData, 0, stdinData.Length);
                    }
                }

                // Setup cancellation token to kill process
                // This has to be after process start so that it can actually be killed
                // and also after standard input so that it can write correctly
                linkedToken.Register(() => process.TryKill());

                // Wait until exit
                processMre.Wait(linkedToken);
                var exitTime = DateTimeOffset.Now;

                // Wait until stdout and stderr finished reading
                stdOutMre.Wait(linkedToken);
                stdErrMre.Wait(linkedToken);

                // Get stdout and stderr
                var stdOut = stdOutBuffer.ToString();
                var stdErr = stdErrBuffer.ToString();

                return new ExecutionOutput(process.ExitCode, stdOut, stdErr, startTime, exitTime);
            }
        }

        /// <summary>
        /// Executes target process with given command line arguments, waits until completion synchronously and returns produced output.
        /// </summary>
        /// <param name="arguments">Command line arguments passed when executing the target process.</param>
        /// <param name="cancellationToken">Token that can be used to abort execution.</param>
        /// <param name="bufferHandler">Handler for real-time standard output and standard error data.</param>
        /// <remarks>The underlying process is killed if the execution is canceled.</remarks>
        public ExecutionOutput Execute(string arguments,
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null)
            => Execute(new ExecutionInput(arguments), cancellationToken, bufferHandler);

        /// <summary>
        /// Executes target process without input, waits until completion synchronously and returns produced output.
        /// </summary>
        /// <param name="cancellationToken">Token that can be used to abort execution.</param>
        /// <param name="bufferHandler">Handler for real-time standard output and standard error data.</param>
        /// <remarks>The underlying process is killed if the execution is canceled.</remarks>
        public ExecutionOutput Execute(
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null)
            => Execute(ExecutionInput.Empty, cancellationToken, bufferHandler);

        #endregion

        #region ExecuteAndForget

        /// <summary>
        /// Executes target process with given input, without waiting for completion.
        /// </summary>
        /// <param name="input">Execution input.</param>
        public void ExecuteAndForget(ExecutionInput input)
        {
            input.GuardNotNull(nameof(input));

            // Create process
            using (var process = CreateProcess(input))
            {
                // Start process
                process.Start();

                // Write stdin
                using (process.StandardInput)
                {
                    if (input.StandardInput != null)
                    {
                        var stdinData = Settings.Encoding.StandardInput.GetBytes(input.StandardInput);
                        var stdinStream = process.StandardInput.BaseStream;
                        stdinStream.Write(stdinData, 0, stdinData.Length);
                    }
                }
            }
        }

        /// <summary>
        /// Executes target process with given command line arguments, without waiting for completion.
        /// </summary>
        /// <param name="arguments">Command line arguments passed when executing the target process.</param>
        public void ExecuteAndForget(string arguments)
            => ExecuteAndForget(new ExecutionInput(arguments));

        /// <summary>
        /// Executes target process without input, without waiting for completion.
        /// </summary>
        public void ExecuteAndForget()
            => ExecuteAndForget(ExecutionInput.Empty);

        #endregion

        #region ExecuteAsync

        /// <summary>
        /// Executes target process with given input, waits until completion asynchronously and returns produced output.
        /// </summary>
        /// <param name="input">Execution input.</param>
        /// <param name="cancellationToken">Token that can be used to abort execution.</param>
        /// <param name="bufferHandler">Handler for real-time standard output and standard error data.</param>
        /// <remarks>The underlying process is killed if the execution is canceled.</remarks>
        public async Task<ExecutionOutput> ExecuteAsync(ExecutionInput input,
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null)
        {
            input.GuardNotNull(nameof(input));

            // Create task completion sources
            var processTcs = new TaskCompletionSource<object>();
            var stdOutTcs = new TaskCompletionSource<object>();
            var stdErrTcs = new TaskCompletionSource<object>();

            // Set up execution context
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _killSwitchCts.Token))
            using (var process = CreateProcess(input))
            {
                // Get linked cancellation token
                var linkedToken = linkedCts.Token;

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
                        bufferHandler?.HandleStandardOutput(args.Data);
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
                        bufferHandler?.HandleStandardError(args.Data);
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
                    if (input.StandardInput != null)
                    {
                        var stdinData = Settings.Encoding.StandardInput.GetBytes(input.StandardInput);
                        var stdinStream = process.StandardInput.BaseStream;
                        await stdinStream.WriteAsync(stdinData, 0, stdinData.Length, linkedToken).ConfigureAwait(false);
                    }
                }

                // Setup cancellation token to kill process and cancel tasks
                // This has to be after process start so that it can actually be killed
                // and also after standard input so that it can write correctly
                linkedToken.Register(() =>
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

                return new ExecutionOutput(process.ExitCode, stdOut, stdErr, startTime, exitTime);
            }
        }

        /// <summary>
        /// Executes target process with given command line arguments, waits until completion asynchronously and returns produced output.
        /// </summary>
        /// <param name="arguments">Command line arguments passed when executing the target process.</param>
        /// <param name="cancellationToken">Token that can be used to abort execution.</param>
        /// <param name="bufferHandler">Handler for real-time standard output and standard error data.</param>
        /// <remarks>The underlying process is killed if the execution is canceled.</remarks>
        public Task<ExecutionOutput> ExecuteAsync(string arguments,
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null)
            => ExecuteAsync(new ExecutionInput(arguments), cancellationToken, bufferHandler);

        /// <summary>
        /// Executes target process without input, waits until completion asynchronously and returns produced output.
        /// </summary>
        /// <param name="cancellationToken">Token that can be used to abort execution.</param>
        /// <param name="bufferHandler">Handler for real-time standard output and standard error data.</param>
        /// <remarks>The underlying process is killed if the execution is canceled.</remarks>
        public Task<ExecutionOutput> ExecuteAsync(
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null)
            => ExecuteAsync(ExecutionInput.Empty, cancellationToken, bufferHandler);

        #endregion

        /// <summary>
        /// Cancels all currently running execution tasks.
        /// </summary>
        /// <remarks>Doesn't affect processes instantiated by <see cref="ExecuteAndForget()"/>.</remarks>
        public void CancelAll()
        {
            lock (_lock)
            {
                _killSwitchCts.Cancel();
                _killSwitchCts.Dispose();
                _killSwitchCts = new CancellationTokenSource();
            }
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _killSwitchCts.Dispose();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}