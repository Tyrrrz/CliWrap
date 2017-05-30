using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Models;

namespace CliWrap
{
    /// <summary>
    /// Wrapper for a Command Line Interface
    /// </summary>
    public partial class Cli
    {
        /// <summary>
        /// Target file path
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Working directory
        /// </summary>
        public string WorkingDirectory { get; }

        /// <summary>
        /// Initializes CLI wrapper on a target
        /// </summary>
        public Cli(string filePath, string workingDirectory)
        {
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

        private Process CreateProcess(string arguments)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = FilePath,
                    WorkingDirectory = WorkingDirectory,
                    Arguments = arguments ?? string.Empty,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };
            return process;
        }

        /// <summary>
        /// Executes CLI with given input, waits until completion and returns output
        /// </summary>
        public ExecutionOutput Execute(ExecutionInput input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // Create process
            using (var process = CreateProcess(input.Arguments))
            {
                // Start process
                process.Start();

                // Write stdin
                if (input.StandardInput != null)
                {
                    using (process.StandardInput)
                        process.StandardInput.Write(input.StandardInput);
                }

                // Read stdout
                string stdOut = process.StandardOutput.ReadToEnd();

                // Read stderr
                string stdErr = process.StandardError.ReadToEnd();

                // Wait until exit
                process.WaitForExit();

                return new ExecutionOutput(process.ExitCode, stdOut, stdErr);
            }
        }

        /// <summary>
        /// Executes CLI with given input, waits until completion and returns output
        /// </summary>
        public ExecutionOutput Execute(string arguments)
            => Execute(new ExecutionInput(arguments));

        /// <summary>
        /// Executes CLI without input, waits until completion and returns output
        /// </summary>
        public ExecutionOutput Execute()
            => Execute(ExecutionInput.Empty);

        /// <summary>
        /// Executes CLI with given input, without waiting for completion
        /// </summary>
        public void ExecuteAndForget(ExecutionInput input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // Create process
            using (var process = CreateProcess(input.Arguments))
            {
                // Start process
                process.Start();

                // Write stdin
                if (input.StandardInput != null)
                {
                    using (process.StandardInput)
                        process.StandardInput.Write(input.StandardInput);
                }
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
            using (var process = CreateProcess(input.Arguments))
            {
                // Wire an event that signals task completion
                process.Exited += (sender, args) => tcs.TrySetResult(null);

                // Start process
                process.Start();

                // Setup cancellation token
                if (cancellationToken != CancellationToken.None)
                {
                    cancellationToken.Register(() =>
                    {
                        // Cancel task
                        tcs.TrySetCanceled();

                        // Kill process
                        // ReSharper disable once AccessToDisposedClosure (exception-safe)
                        TryKillProcess(process);
                    });
                }

                // Write stdin
                if (input.StandardInput != null)
                {
                    using (process.StandardInput)
                        await process.StandardInput.WriteAsync(input.StandardInput);
                }

                // Read stdout
                string stdOut = await process.StandardOutput.ReadToEndAsync();

                // Read stderr
                string stdErr = await process.StandardError.ReadToEndAsync();

                // Wait until exit
                await tcs.Task;

                return new ExecutionOutput(process.ExitCode, stdOut, stdErr);
            }
        }

        /// <summary>
        /// Executes CLI with given input, waits until completion asynchronously and returns output
        /// </summary>
        public async Task<ExecutionOutput> ExecuteAsync(ExecutionInput input)
            => await ExecuteAsync(input, CancellationToken.None);

        /// <summary>
        /// Executes CLI with given input, waits until completion asynchronously and returns output
        /// </summary>
        public async Task<ExecutionOutput> ExecuteAsync(string arguments, CancellationToken cancellationToken)
            => await ExecuteAsync(new ExecutionInput(arguments), cancellationToken);

        /// <summary>
        /// Executes CLI with given input, waits until completion asynchronously and returns output
        /// </summary>
        public async Task<ExecutionOutput> ExecuteAsync(string arguments)
            => await ExecuteAsync(new ExecutionInput(arguments), CancellationToken.None);

        /// <summary>
        /// Executes CLI without input, waits until completion asynchronously and returns output
        /// </summary>
        public async Task<ExecutionOutput> ExecuteAsync(CancellationToken cancellationToken)
            => await ExecuteAsync(ExecutionInput.Empty, cancellationToken);

        /// <summary>
        /// Executes CLI without input, waits until completion asynchronously and returns output
        /// </summary>
        public async Task<ExecutionOutput> ExecuteAsync()
            => await ExecuteAsync(ExecutionInput.Empty, CancellationToken.None);
    }

    public partial class Cli
    {
        private static void TryKillProcess(Process process)
        {
            if (process == null)
                return;

            try
            {
                process.Kill();
            }
            catch
            {
                // Ignored
            }
        }
    }
}