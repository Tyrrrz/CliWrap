using System;
using System.Diagnostics;
using System.IO;
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
    public class Cli
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

        private Process CreateProcess(ExecutionInput execInput)
        {
            var retProcess = new Process
            {
                StartInfo =
                {
                    FileName = FilePath,
                    WorkingDirectory = WorkingDirectory,
                    Arguments = execInput.Arguments,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            if (execInput.EnvironmentVariables != null)
            {
                foreach (var item in execInput.EnvironmentVariables)
                {
#if NET45 || NETSTANDARD2_0
                    retProcess.StartInfo.EnvironmentVariables.Add(item.Key, item.Value);
#endif
                }
            }

            return retProcess;
        }

        /// <summary>
        /// Executes CLI with given input, waits until completion and returns output
        /// </summary>
        public ExecutionOutput Execute(ExecutionInput input, CancellationToken cancellationToken)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // Create process
            using (var process = CreateProcess(input.Arguments, input.EnvironmentVariables))
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

                // Write stdin
                using (process.StandardInput)
                    process.StandardInput.Write(input.StandardInput);

                // Setup cancellation token
                // This has to be after process start so that it can actually be killed
                // and also after standard input so that it can write correctly
                cancellationToken.Register(() =>
                {
                    // Kill process if it's not dead already
                    // ReSharper disable once AccessToDisposedClosure
                    process.KillIfRunning();
                });

                // Begin reading stdout and stderr
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait until exit
                process.WaitForExit();

                // Check cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Get stdout and stderr
                var stdOut = stdOutBuffer.ToString();
                var stdErr = stdErrBuffer.ToString();

                return new ExecutionOutput(process.ExitCode, stdOut, stdErr);
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
            using (var process = CreateProcess(input.Arguments, input.EnvironmentVariables))
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
            using (var process = CreateProcess(input.Arguments, input.EnvironmentVariables))
            {
                // Wire an event that signals task completion
                process.Exited += (sender, args) => tcs.SetResult(null);

                // Start process
                process.Start();

                // Write stdin
                using (process.StandardInput)
                    await process.StandardInput.WriteAsync(input.StandardInput).ConfigureAwait(false);

                // Setup cancellation token
                // This has to be after process start so that it can actually be killed
                // and also after standard input so that it can write correctly
                cancellationToken.Register(() =>
                {
                    // Kill process if it's not dead already
                    // ReSharper disable once AccessToDisposedClosure
                    process.KillIfRunning();
                });

                // Begin reading stdout and stderr
                var stdOutReadTask = process.StandardOutput.ReadToEndAsync();
                var stdErrReadTask = process.StandardError.ReadToEndAsync();

                // Wait until exit
                await tcs.Task.ConfigureAwait(false);

                // Check cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Get stdout and stderr
                var stdOut = await stdOutReadTask.ConfigureAwait(false);
                var stdErr = await stdErrReadTask.ConfigureAwait(false);

                return new ExecutionOutput(process.ExitCode, stdOut, stdErr);
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
    }
}
