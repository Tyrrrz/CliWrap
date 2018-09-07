using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
    public class Cli : ICli
    {
        #region InteropServices
        internal const int CTRL_C_EVENT = 0;
        [DllImport("kernel32.dll")]
        internal static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool AttachConsole(uint dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        internal static extern bool FreeConsole();
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);
        // Delegate type to be used as the Handler Routine for SCCH
        delegate Boolean ConsoleCtrlDelegate(uint CtrlType);
        #endregion

        private readonly object _lock = new object();
        private bool _isDisposed;
        private CancellationTokenSource _killSwitchCts;

        /// <inheritdoc />
        public string FilePath { get; }

        /// <inheritdoc />
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

            // Create kill switch
            _killSwitchCts = new CancellationTokenSource();
        }

        /// <summary>
        /// Initializes wrapper on a target executable with default settings.
        /// </summary>
        /// <param name="filePath">File path of the target executable.</param>
        public Cli(string filePath)
            : this(filePath, new CliSettings())
        {
        }

        private CancellationTokenSource LinkCancellationToken(CancellationToken cancellationToken)
        {
            return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _killSwitchCts.Token);
        }

        private Process CreateProcess(ExecutionInput input)
        {
            // Create process start info
            var startInfo = new ProcessStartInfo
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
            };

            // Set environment variables
            startInfo.SetEnvironmentVariables(input.EnvironmentVariables);

            // Create process
            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            return process;
        }

        #region Execute

        /// <inheritdoc />
        public ExecutionOutput Execute(ExecutionInput input,
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null)
        {
            input.GuardNotNull(nameof(input));

            // Check if disposed
            ThrowIfDisposed();

            // Set up execution context
            using (var processMre = new ManualResetEventSlim())
            using (var stdOutMre = new ManualResetEventSlim())
            using (var stdErrMre = new ManualResetEventSlim())
            using (var linkedCts = LinkCancellationToken(cancellationToken))
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

                // Setup cancellation token to kill process and set events
                // This has to be after process start so that it can actually be killed
                // and also after standard input so that it can write correctly
                linkedToken.Register(() =>
                {
                    TryKillProcess(process);
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
                linkedToken.ThrowIfCancellationRequested();

                // Get stdout and stderr
                var stdOut = stdOutBuffer.ToString();
                var stdErr = stdErrBuffer.ToString();

                return new ExecutionOutput(process.ExitCode, stdOut, stdErr, startTime, exitTime);
            }
        }

        private void TryKillProcess(Process process)
        {
            try
            {
                if(Settings.IsSoftKillEnabled)
                {
                    if(AttachConsole((uint)process.Id))
                    {
                        SetConsoleCtrlHandler(null, true);
                        if(!GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0))
                        {
                            return;
                        }
                    }
                }
                else
                {
                    process.Kill();
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc);
            }
            finally
            {
                FreeConsole();
                SetConsoleCtrlHandler(null, false);
            }
        }

        /// <inheritdoc />
        public ExecutionOutput Execute(string arguments,
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null)
            => Execute(new ExecutionInput(arguments), cancellationToken, bufferHandler);

        /// <inheritdoc />
        public ExecutionOutput Execute(
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null)
            => Execute(new ExecutionInput(), cancellationToken, bufferHandler);

        #endregion

        #region ExecuteAndForget

        /// <inheritdoc />
        public void ExecuteAndForget(ExecutionInput input)
        {
            input.GuardNotNull(nameof(input));

            // Check if disposed
            ThrowIfDisposed();

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

        /// <inheritdoc />
        public void ExecuteAndForget(string arguments)
            => ExecuteAndForget(new ExecutionInput(arguments));

        /// <inheritdoc />
        public void ExecuteAndForget()
            => ExecuteAndForget(new ExecutionInput());

        #endregion

        #region ExecuteAsync

        /// <inheritdoc />
        public async Task<ExecutionOutput> ExecuteAsync(ExecutionInput input,
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null)
        {
            input.GuardNotNull(nameof(input));

            // Check if disposed
            ThrowIfDisposed();

            // Create task completion sources
            var processTcs = new TaskCompletionSource<object>();
            var stdOutTcs = new TaskCompletionSource<object>();
            var stdErrTcs = new TaskCompletionSource<object>();

            // Set up execution context
            using (var linkedCts = LinkCancellationToken(cancellationToken))
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
                    TryKillProcess(process);
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

        /// <inheritdoc />
        public Task<ExecutionOutput> ExecuteAsync(string arguments,
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null)
            => ExecuteAsync(new ExecutionInput(arguments), cancellationToken, bufferHandler);

        /// <inheritdoc />
        public Task<ExecutionOutput> ExecuteAsync(
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null)
            => ExecuteAsync(new ExecutionInput(), cancellationToken, bufferHandler);

        #endregion

        /// <inheritdoc />
        public void CancelAll()
        {
            // Check if disposed
            ThrowIfDisposed();

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
            if (disposing && !_isDisposed)
            {
                _isDisposed = true;
                _killSwitchCts.Dispose();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().ToString());
        }
    }
}