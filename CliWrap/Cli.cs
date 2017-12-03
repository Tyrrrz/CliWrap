using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    public class Cli
    {
        private readonly HashSet<Process> _processes;

        /// <summary>
        /// Target executable file path.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Working directory.
        /// </summary>
        public string WorkingDirectory { get; }

        /// <summary>
        /// Initializes wrapper on a target executable using given working directory.
        /// </summary>
        /// <param name="filePath">File path of the target executable.</param>
        /// <param name="workingDirectory">Target executable's working directory.</param>
        public Cli(string filePath, string workingDirectory)
        {
            FilePath = filePath.GuardNotNull(nameof(filePath));
            WorkingDirectory = workingDirectory.GuardNotNull(nameof(workingDirectory));

            _processes = new HashSet<Process>();
        }

        /// <summary>
        /// Initializes wrapper on a target executable using current directory as working directory.
        /// </summary>
        /// <param name="filePath">File path of the target executable.</param>
        public Cli(string filePath)
            : this(filePath, Directory.GetCurrentDirectory())
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
                    WorkingDirectory = WorkingDirectory,
                    Arguments = input.Arguments,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            // Set environment variables
#if NET45
            foreach (var variable in input.EnvironmentVariables)                
                process.StartInfo.EnvironmentVariables.Add(variable.Key, variable.Value);
#else
            foreach (var variable in input.EnvironmentVariables)
                process.StartInfo.Environment.Add(variable.Key, variable.Value);
#endif

            return process;
        }

        #region Execute

        /// <summary>
        /// Executes target process with given input, waits until completion synchronously and returns produced output.
        /// </summary>
        /// <param name="input">Execution input.</param>
        /// <param name="cancellationToken">Token that can be used to abort execution.</param>
        /// <param name="bufferHandler">Handler for realtime standard output and standard error data.</param>
        /// <exception cref="OperationCanceledException">Thrown if the execution was canceled.</exception>
        /// <remarks>The underlying process is killed if the execution is canceled.</remarks>
        public ExecutionOutput Execute(ExecutionInput input,
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null)
        {
            input.GuardNotNull(nameof(input));

            // Create process
            using (var process = CreateProcess(input))
            {
                // Create buffers
                var stdOutBuffer = new StringBuilder();
                var stdErrBuffer = new StringBuilder();

                // Wire events
                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        stdOutBuffer.AppendLine(args.Data);
                        bufferHandler?.HandleStandardOutput(args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        stdErrBuffer.AppendLine(args.Data);
                        bufferHandler?.HandleStandardError(args.Data);
                    }
                };

                // Start process
                process.Start();
                _processes.Add(process);

                // Write stdin
                using (process.StandardInput)
                    process.StandardInput.Write(input.StandardInput);

                // Setup cancellation token
                // This has to be after process start so that it can actually be killed
                // and also after standard input so that it can write correctly
                cancellationToken.Register(() =>
                {
                    // Kill process if it's not dead already
                    process.KillIfRunning();
                });

                // Begin reading stdout and stderr
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait until exit
                process.WaitForExit();
                _processes.Remove(process);

                // Check cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Get stdout and stderr
                var stdOut = stdOutBuffer.ToString();
                var stdErr = stdErrBuffer.ToString();

                return new ExecutionOutput(process.ExitCode, stdOut, stdErr, process.StartTime, process.ExitTime);
            }
        }

        /// <summary>
        /// Executes target process with given command line arguments, waits until completion synchronously and returns produced output.
        /// </summary>
        /// <param name="arguments">Command line arguments passed when executing the target process.</param>
        /// <param name="cancellationToken">Token that can be used to abort execution.</param>
        /// <param name="bufferHandler">Handler for realtime standard output and standard error data.</param>
        /// <exception cref="OperationCanceledException">Thrown if the execution was canceled.</exception>
        /// <remarks>The underlying process is killed if the execution is canceled.</remarks>
        public ExecutionOutput Execute(string arguments,
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null)
            => Execute(new ExecutionInput(arguments), cancellationToken, bufferHandler);

        /// <summary>
        /// Executes target process without input, waits until completion synchronously and returns produced output.
        /// </summary>
        /// <param name="cancellationToken">Token that can be used to abort execution.</param>
        /// <param name="bufferHandler">Handler for realtime standard output and standard error data.</param>
        /// <exception cref="OperationCanceledException">Thrown if the execution was canceled.</exception>
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
                    process.StandardInput.Write(input.StandardInput);
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
        /// <param name="bufferHandler">Handler for realtime standard output and standard error data.</param>
        /// <exception cref="OperationCanceledException">Thrown if the execution was canceled.</exception>
        /// <remarks>The underlying process is killed if the execution is canceled.</remarks>
        public async Task<ExecutionOutput> ExecuteAsync(ExecutionInput input,
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null)
        {
            input.GuardNotNull(nameof(input));

            // Create task completion source
            var tcs = new TaskCompletionSource<object>();

            // Create process
            using (var process = CreateProcess(input))
            {
                // Wire events
                process.Exited += (sender, args) => tcs.SetResult(null);

                // Create buffers
                var stdOutBuffer = new StringBuilder();
                var stdErrBuffer = new StringBuilder();

                // Wire events
                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        stdOutBuffer.AppendLine(args.Data);
                        bufferHandler?.HandleStandardOutput(args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        stdErrBuffer.AppendLine(args.Data);
                        bufferHandler?.HandleStandardError(args.Data);
                    }
                };

                // Start process
                process.Start();
                _processes.Add(process);

                // Write stdin
                using (process.StandardInput)
                    await process.StandardInput.WriteAsync(input.StandardInput).ConfigureAwait(false);

                // Setup cancellation token
                // This has to be after process start so that it can actually be killed
                // and also after standard input so that it can write correctly
                cancellationToken.Register(() =>
                {
                    // Kill process if it's not dead already
                    process.KillIfRunning();
                });

                // Begin reading stdout and stderr
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait until exit
                await tcs.Task.ConfigureAwait(false);
                _processes.Remove(process);

                // Check cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Get stdout and stderr
                var stdOut = stdOutBuffer.ToString();
                var stdErr = stdErrBuffer.ToString();

                return new ExecutionOutput(process.ExitCode, stdOut, stdErr, process.StartTime, process.ExitTime);
            }
        }

        /// <summary>
        /// Executes target process with given command line arguments, waits until completion asynchronously and returns produced output.
        /// </summary>
        /// <param name="arguments">Command line arguments passed when executing the target process.</param>
        /// <param name="cancellationToken">Token that can be used to abort execution.</param>
        /// <param name="bufferHandler">Handler for realtime standard output and standard error data.</param>
        /// <exception cref="OperationCanceledException">Thrown if the execution was canceled.</exception>
        /// <remarks>The underlying process is killed if the execution is canceled.</remarks>
        public Task<ExecutionOutput> ExecuteAsync(string arguments,
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null)
            => ExecuteAsync(new ExecutionInput(arguments), cancellationToken, bufferHandler);

        /// <summary>
        /// Executes target process without input, waits until completion asynchronously and returns produced output.
        /// </summary>
        /// <param name="cancellationToken">Token that can be used to abort execution.</param>
        /// <param name="bufferHandler">Handler for realtime standard output and standard error data.</param>
        /// <exception cref="OperationCanceledException">Thrown if the execution was canceled.</exception>
        /// <remarks>The underlying process is killed if the execution is canceled.</remarks>
        public Task<ExecutionOutput> ExecuteAsync(
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null)
            => ExecuteAsync(ExecutionInput.Empty, cancellationToken, bufferHandler);

        #endregion

        /// <summary>
        /// Kills all currently running underlying processes created by this instance of <see cref="Cli" />.
        /// </summary>
        /// <exception cref="AggregateException">Thrown if one or more processes were not killed successfully.</exception>
        /// <remarks>Processes executed by <see cref="ExecuteAndForget()"/> are not killed.</remarks>
        public void KillAllProcesses()
        {
            var exceptions = new List<Exception>();

            // Try to kill as many processes as possible
            foreach (var process in _processes.ToArray())
            {
                try
                {
                    process.KillIfRunning();
                    _processes.Remove(process);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            // Throw an aggregate exception if necessary
            if (exceptions.Any())
                throw new AggregateException("At least some processes were not killed successfully.", exceptions);
        }
    }
}
