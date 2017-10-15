using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Internal;
using CliWrap.Models;
using System.Text;
using System.Collections.Generic;

namespace CliWrap
{
    /// <summary>
    /// Wrapper for a Command Line Interface
    /// </summary>
    public class Cli : IDisposable
    {
        private readonly HashSet<Process> _processes;

        /// <summary>
        /// Target file path
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Working directory
        /// </summary>
        public string WorkingDirectory { get; }

        /// <summary>
        /// List of all currently running processes
        /// </summary>
        private static IList<Process> RunningProcesses = new List<Process>();

#if NET45
        /// <summary>
        /// Whether to kill all the currently running child processes when the parent process exits
        /// </summary>
        public static bool KillChildProcessesOnParentProcessExit { get; set; } = true;

        static Cli()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => KillChildren();
        }
#endif

        /// <summary>
        /// Try to kill all running child processes
        /// </summary>
        public static void KillChildren()
        {
            bool kill = true;

#if NET45
            kill = KillChildProcessesOnParentProcessExit;
#endif

            if (kill)
            {
                foreach (var item in RunningProcesses)
                {
                    if (item.KillIfRunning())
                    {
                        RunningProcesses.Remove(item);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes CLI wrapper on a target
        /// </summary>
        public Cli(string filePath, string workingDirectory)
        {
            _processes = new HashSet<Process>();

            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            WorkingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
        }

        /// <summary>
        /// Initializes CLI wrapper on a target
        /// </summary>
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

#if NET45 || NETSTANDARD2_0
            // Set environment variables
            foreach (var variable in input.EnvironmentVariables)
            {
                process.StartInfo.EnvironmentVariables.Add(variable.Key, variable.Value);
            }
#endif

            return process;
        }

        /// <summary>
        /// Executes CLI with given input, waits until completion and returns output
        /// </summary>
        public ExecutionOutput Execute(ExecutionInput input, CancellationToken cancellationToken)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

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
                        stdOutBuffer.AppendLine(args.Data);
                };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                        stdErrBuffer.AppendLine(args.Data);
                };

                // Start process
                process.Start();
                _processes.Add(process);

                //Add process to the running processes list
                RunningProcesses.Add(process);

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

                //Remove process from running list
                RunningProcesses.Remove(process);

                // Check cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Get stdout and stderr
                var stdOut = stdOutBuffer.ToString();
                var stdErr = stdErrBuffer.ToString();

                return new ExecutionOutput(process.ExitCode, stdOut, stdErr, process.StartTime, process.ExitTime);
            }
        }

        /// <summary>
        /// Executes CLI with given input, waits until completion and returns output
        /// </summary>
        public ExecutionOutput Execute(ExecutionInput input)
            => Execute(input, CancellationToken.None);

        /// <summary>
        /// Executes CLI with given input, waits until completion and returns output
        /// </summary>
        public ExecutionOutput Execute(string arguments, CancellationToken cancellationToken)
            => Execute(new ExecutionInput(arguments), cancellationToken);

        /// <summary>
        /// Executes CLI with given input, waits until completion and returns output
        /// </summary>
        public ExecutionOutput Execute(string arguments)
            => Execute(new ExecutionInput(arguments), CancellationToken.None);

        /// <summary>
        /// Executes CLI without input, waits until completion and returns output
        /// </summary>
        public ExecutionOutput Execute(CancellationToken cancellationToken)
            => Execute(ExecutionInput.Empty, cancellationToken);

        /// <summary>
        /// Executes CLI without input, waits until completion and returns output
        /// </summary>
        public ExecutionOutput Execute()
            => Execute(ExecutionInput.Empty, CancellationToken.None);

        /// <summary>
        /// Executes CLI with given input, without waiting for completion
        /// </summary>
        public void ExecuteAndForget(ExecutionInput input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

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
        /// Executes CLI with given input, without waiting for completion
        /// </summary>
        public void ExecuteAndForget(string arguments)
            => ExecuteAndForget(new ExecutionInput(arguments));

        /// <summary>
        /// Executes CLI without input, without waiting for completion
        /// </summary>
        public void ExecuteAndForget()
            => ExecuteAndForget(ExecutionInput.Empty);

        /// <summary>
        /// Executes CLI with given input, waits until completion asynchronously and returns output
        /// </summary>
        public async Task<ExecutionOutput> ExecuteAsync(ExecutionInput input, CancellationToken cancellationToken)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // Create task completion source
            var tcs = new TaskCompletionSource<object>();

            // Create process
            using (var process = CreateProcess(input))
            {
                // Wire events
                process.Exited += (sender, args) => tcs.SetResult(null);

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
                var stdOutReadTask = process.StandardOutput.ReadToEndAsync();
                var stdErrReadTask = process.StandardError.ReadToEndAsync();

                // Wait until exit
                await tcs.Task.ConfigureAwait(false);
                _processes.Remove(process);

                // Check cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Get stdout and stderr
                var stdOut = await stdOutReadTask.ConfigureAwait(false);
                var stdErr = await stdErrReadTask.ConfigureAwait(false);

                return new ExecutionOutput(process.ExitCode, stdOut, stdErr, process.StartTime, process.ExitTime);
            }
        }

        /// <summary>
        /// Executes CLI with given input, waits until completion asynchronously and returns output
        /// </summary>
        public Task<ExecutionOutput> ExecuteAsync(ExecutionInput input)
            => ExecuteAsync(input, CancellationToken.None);

        /// <summary>
        /// Executes CLI with given input, waits until completion asynchronously and returns output
        /// </summary>
        public Task<ExecutionOutput> ExecuteAsync(string arguments, CancellationToken cancellationToken)
            => ExecuteAsync(new ExecutionInput(arguments), cancellationToken);

        /// <summary>
        /// Executes CLI with given input, waits until completion asynchronously and returns output
        /// </summary>
        public Task<ExecutionOutput> ExecuteAsync(string arguments)
            => ExecuteAsync(new ExecutionInput(arguments), CancellationToken.None);

        /// <summary>
        /// Executes CLI without input, waits until completion asynchronously and returns output
        /// </summary>
        public Task<ExecutionOutput> ExecuteAsync(CancellationToken cancellationToken)
            => ExecuteAsync(ExecutionInput.Empty, cancellationToken);

        /// <summary>
        /// Executes CLI without input, waits until completion asynchronously and returns output
        /// </summary>
        public Task<ExecutionOutput> ExecuteAsync()
            => ExecuteAsync(ExecutionInput.Empty, CancellationToken.None);

        /// <summary>
        /// Kills all currently running child processes created by this instance of <see cref="Cli" />
        /// </summary>
        public void KillAllProcesses()
        {
            foreach (var process in _processes.ToArray())
            {
                process.KillIfRunning();
                _processes.Remove(process);
            }
        }

        /// <summary />
        protected virtual void Dispose(bool disposing)
        {
            KillAllProcesses();
        }

        /// <summary />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary />
        ~Cli()
        {
            Dispose(false);
        }
    }
}