using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Exceptions;

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
            : this(filePath, Environment.CurrentDirectory)
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
                    Arguments = arguments,
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
        /// Execute CLI with given arguments, wait until completion and return standard output
        /// </summary>
        public string Execute(string arguments)
        {
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            // Create process
            using (var process = CreateProcess(arguments))
            {
                // Start process
                process.Start();

                // Read stdout
                string stdOut = process.StandardOutput.ReadToEnd();

                // Read stderr
                string stdErr = process.StandardError.ReadToEnd();

                // Wait until exit
                process.WaitForExit();

                // Check if there is an error
                if (!string.IsNullOrEmpty(stdErr))
                    throw new StdErrException(stdErr);

                return stdOut;
            }
        }

        /// <summary>
        /// Execute CLI without arguments, wait until completion and return standard output
        /// </summary>
        public string Execute()
            => Execute(string.Empty);

        /// <summary>
        /// Execute CLI with given arguments, without waiting for completion
        /// </summary>
        public void ExecuteAndForget(string arguments)
        {
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            // Create process
            using (var process = CreateProcess(arguments))
            {
                // Start process
                process.Start();
            }
        }

        /// <summary>
        /// Execute CLI without arguments, without waiting for completion
        /// </summary>
        public void ExecuteAndForget()
            => ExecuteAndForget(string.Empty);

        /// <summary>
        /// Execute CLI with given arguments, wait until completion asynchronously and return standard output
        /// </summary>
        public async Task<string> ExecuteAsync(string arguments, CancellationToken cancellationToken)
        {
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            // Create process
            using (var process = CreateProcess(arguments))
            {
                // Create task completion source
                var tcs = new TaskCompletionSource<object>();

                // Setup cancellation token
                if (cancellationToken != CancellationToken.None)
                {
                    cancellationToken.Register(() =>
                    {
                        // Cancel task
                        tcs.SetCanceled();

                        // Kill process
                        // ReSharper disable once AccessToDisposedClosure (exception-safe)
                        TryKillProcess(process);
                    });
                }

                // Wire an event that signals task completion
                process.Exited += (sender, args) => tcs.TrySetResult(null);

                // Start process
                process.Start();

                // Read stdout
                string stdOut = await process.StandardOutput.ReadToEndAsync();

                // Read stderr
                string stdErr = await process.StandardError.ReadToEndAsync();

                // Wait until exit
                await tcs.Task;

                // Check if there is an error
                if (!string.IsNullOrEmpty(stdErr))
                    throw new StdErrException(stdErr);

                return stdOut;
            }
        }

        /// <summary>
        /// Execute CLI with given arguments, wait until completion asynchronously and return standard output
        /// </summary>
        public async Task<string> ExecuteAsync(string arguments)
            => await ExecuteAsync(arguments, CancellationToken.None);

        /// <summary>
        /// Execute CLI without arguments, wait until completion asynchronously and return standard output
        /// </summary>
        public async Task<string> ExecuteAsync(CancellationToken cancellationToken)
            => await ExecuteAsync(string.Empty, cancellationToken);

        /// <summary>
        /// Execute CLI without arguments, wait until completion asynchronously and return standard output
        /// </summary>
        public async Task<string> ExecuteAsync()
            => await ExecuteAsync(string.Empty);
    }

    public partial class Cli
    {
        private static void TryKillProcess(Process process)
        {
            try
            {
                process?.Kill();
            }
            catch
            {
                // Ignored
            }
        }
    }
}