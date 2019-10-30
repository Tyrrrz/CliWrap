using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private Stream _standardInput = Stream.Null;
        private readonly IDictionary<string, string> _environmentVariables = new Dictionary<string, string>();
        private Encoding _standardOutputEncoding = Console.OutputEncoding;
        private Encoding _standardErrorEncoding = Console.OutputEncoding;
        private Action<string> _standardOutputObserver;
        private Action _standardOutputClosedObserver;
        private Action<string> _standardErrorObserver;
        private Action _standardErrorClosedObserver;
        private CancellationToken _cancellationToken;
        private bool _killEntireProcessTree;
        private bool _exitCodeValidation = true;
        private bool _standardErrorValidation;

        /// <inheritdoc />
        public int? ProcessId { get; private set; }

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
        public ICli SetArguments(IReadOnlyList<string> arguments)
        {
            arguments.GuardNotNull(nameof(arguments));

            var buffer = new StringBuilder();

            foreach (var argument in arguments)
            {
                // If buffer has something in it - append a space
                if (buffer.Length != 0)
                    buffer.Append(' ');

                // If argument is clean and doesn't need escaping - append it directly
                if (argument.Length != 0 && argument.All(c => !char.IsWhiteSpace(c) && c != '"'))
                {
                    buffer.Append(argument);
                }
                // Otherwise - escape problematic characters
                else
                {
                    // Escaping logic taken from CoreFx source code

                    buffer.Append('"');

                    for (var i = 0; i < argument.Length;)
                    {
                        var c = argument[i++];

                        if (c == '\\')
                        {
                            var numBackSlash = 1;
                            while (i < argument.Length && argument[i] == '\\')
                            {
                                numBackSlash++;
                                i++;
                            }

                            if (i == argument.Length)
                            {
                                buffer.Append('\\', numBackSlash * 2);
                            }
                            else if (argument[i] == '"')
                            {
                                buffer.Append('\\', numBackSlash * 2 + 1);
                                buffer.Append('"');
                                i++;
                            }
                            else
                            {
                                buffer.Append('\\', numBackSlash);
                            }
                        }
                        else if (c == '"')
                        {
                            buffer.Append('\\');
                            buffer.Append('"');
                        }
                        else
                        {
                            buffer.Append(c);
                        }
                    }

                    buffer.Append('"');
                }
            }

            return SetArguments(buffer.ToString());
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
            var data = encoding.GetBytes(standardInput);
            var stream = new MemoryStream(data, false);

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
        public ICli SetStandardOutputClosedCallback(Action callback)
        {
            _standardOutputClosedObserver = callback.GuardNotNull(nameof(callback));
            return this;
        }

        /// <inheritdoc />
        public ICli SetStandardErrorCallback(Action<string> callback)
        {
            _standardErrorObserver = callback.GuardNotNull(nameof(callback));
            return this;
        }

        /// <inheritdoc />
        public ICli SetStandardErrorClosedCallback(Action callback)
        {
            _standardErrorClosedObserver = callback.GuardNotNull(nameof(callback));
            return this;
        }

        /// <inheritdoc />
        public ICli SetCancellationToken(CancellationToken token, bool killEntireProcessTree)
        {
            _cancellationToken = token;
            _killEntireProcessTree = killEntireProcessTree;
            return this;
        }

        /// <inheritdoc />
        public ICli SetCancellationToken(CancellationToken token) => SetCancellationToken(token, false);

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

        private CliProcess StartProcess()
        {
            // Create process start info
            var startInfo = new ProcessStartInfo
            {
                FileName = _filePath,
                WorkingDirectory = _workingDirectory,
                Arguments = _arguments,
                StandardOutputEncoding = _standardOutputEncoding,
                StandardErrorEncoding = _standardErrorEncoding
            };

            // Set environment variables
#if NET45
            foreach (var variable in _environmentVariables)
                startInfo.EnvironmentVariables[variable.Key] = variable.Value;
#else
            foreach (var variable in _environmentVariables)
                startInfo.Environment[variable.Key] = variable.Value;
#endif

            // Create and start process
            var process = new CliProcess(
                startInfo,
                _standardOutputObserver,
                _standardErrorObserver,
                _standardOutputClosedObserver,
                _standardErrorClosedObserver
            );
            process.Start();

            return process;
        }

        private void ValidateExecutionResult(ExecutionResult result)
        {
            // Validate exit code if needed
            if (_exitCodeValidation && result.ExitCode != 0)
                throw new ExitCodeValidationException(result);

            // Validate standard error if needed
            if (_standardErrorValidation && !result.StandardError.IsNullOrWhiteSpace())
                throw new StandardErrorValidationException(result);
        }

        /// <inheritdoc />
        public ExecutionResult Execute()
        {
            // Set up execution context
            using (var process = StartProcess())
            using (_cancellationToken.Register(() => process.TryKill(_killEntireProcessTree)))
            {
                ProcessId = process.Id;

                // Pipe stdin
                process.PipeStandardInput(_standardInput);

                // Wait for exit
                process.WaitForExit();

                // Throw if cancelled
                _cancellationToken.ThrowIfCancellationRequested();

                // Create execution result
                var result = new ExecutionResult(process.ExitCode,
                    process.StandardOutput,
                    process.StandardError,
                    process.StartTime,
                    process.ExitTime);

                // Validate execution result
                ValidateExecutionResult(result);

                return result;
            }
        }

        /// <inheritdoc />
        public async Task<ExecutionResult> ExecuteAsync()
        {
            // Set up execution context
            using (var process = StartProcess())
            using (_cancellationToken.Register(() => process.TryKill(_killEntireProcessTree)))
            {
                ProcessId = process.Id;

                // Pipe stdin
                await process.PipeStandardInputAsync(_standardInput);

                // Wait for exit
                await process.WaitForExitAsync();

                // Throw if cancelled
                _cancellationToken.ThrowIfCancellationRequested();

                // Create execution result
                var result = new ExecutionResult(process.ExitCode,
                    process.StandardOutput,
                    process.StandardError,
                    process.StartTime,
                    process.ExitTime);

                // Validate execution result
                ValidateExecutionResult(result);

                return result;
            }

        }

        /// <inheritdoc />
        public void ExecuteAndForget()
        {
            // Set up execution context
            using (var process = StartProcess())
            {
                ProcessId = process.Id;

                // Write stdin
                process.PipeStandardInput(_standardInput);
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