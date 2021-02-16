using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Builders;
using CliWrap.Exceptions;
using CliWrap.Utils;
using CliWrap.Utils.Extensions;

namespace CliWrap
{
    /// <summary>
    /// Represents a shell command.
    /// </summary>
    public partial class Command : ICommandConfiguration
    {
        /// <summary>
        /// File path of the executable, batch file, or script, that this command runs on.
        /// </summary>
        public string TargetFilePath { get; }

        /// <summary>
        /// Arguments passed on the command line.
        /// </summary>
        public string Arguments { get; }

        /// <summary>
        /// Working directory path.
        /// </summary>
        public string WorkingDirPath { get; }

        /// <summary>
        /// User credentials set for the underlying process.
        /// </summary>
        public Credentials Credentials { get; }

        /// <summary>
        /// Environment variables set for the underlying process.
        /// </summary>
        public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

        /// <summary>
        /// Configured result validation options.
        /// </summary>
        public CommandResultValidation Validation { get; }

        /// <summary>
        /// Configured standard input pipe source.
        /// </summary>
        public PipeSource StandardInputPipe { get; }

        /// <summary>
        /// Configured standard output pipe target.
        /// </summary>
        public PipeTarget StandardOutputPipe { get; }

        /// <summary>
        /// Configured standard error pipe target.
        /// </summary>
        public PipeTarget StandardErrorPipe { get; }

        /// <summary>
        /// Initializes an instance of <see cref="Command"/>.
        /// </summary>
        public Command(
            string targetFilePath,
            string arguments,
            string workingDirPath,
            Credentials credentials,
            IReadOnlyDictionary<string, string> environmentVariables,
            CommandResultValidation validation,
            PipeSource standardInputPipe,
            PipeTarget standardOutputPipe,
            PipeTarget standardErrorPipe)
        {
            TargetFilePath = targetFilePath;
            Arguments = arguments;
            WorkingDirPath = workingDirPath;
            Credentials = credentials;
            EnvironmentVariables = environmentVariables;
            Validation = validation;
            StandardInputPipe = standardInputPipe;
            StandardOutputPipe = standardOutputPipe;
            StandardErrorPipe = standardErrorPipe;
        }

        /// <summary>
        /// Initializes an instance of <see cref="Command"/>.
        /// </summary>
        public Command(string targetFilePath) : this(
            targetFilePath,
            string.Empty,
            Directory.GetCurrentDirectory(),
            Credentials.Default,
            new Dictionary<string, string>(),
            CommandResultValidation.ZeroExitCode,
            PipeSource.Null,
            PipeTarget.Null,
            PipeTarget.Null)
        {
        }

        /// <summary>
        /// Creates a copy of this command, setting the arguments to the specified value.
        /// </summary>
        public Command WithArguments(string arguments) => new(
            TargetFilePath,
            arguments,
            WorkingDirPath,
            Credentials,
            EnvironmentVariables,
            Validation,
            StandardInputPipe,
            StandardOutputPipe,
            StandardErrorPipe
        );

        /// <summary>
        /// Creates a copy of this command, setting the arguments to the value configured by the specified delegate.
        /// </summary>
        public Command WithArguments(Action<ArgumentsBuilder> configure)
        {
            var builder = new ArgumentsBuilder();
            configure(builder);

            return WithArguments(builder.Build());
        }

        /// <summary>
        /// Creates a copy of this command, setting the arguments to the value obtained by formatting the specified enumeration.
        /// </summary>
        public Command WithArguments(IEnumerable<string> arguments, bool escape) =>
            WithArguments(args => args.Add(arguments, escape));

        /// <summary>
        /// Creates a copy of this command, setting the arguments to the value obtained by formatting the specified enumeration.
        /// </summary>
        // TODO: (breaking change) remove in favor of optional parameter
        public Command WithArguments(IEnumerable<string> arguments) =>
            WithArguments(arguments, true);

        /// <summary>
        /// Creates a copy of this command, setting the working directory path to the specified value.
        /// </summary>
        public Command WithWorkingDirectory(string workingDirPath) => new(
            TargetFilePath,
            Arguments,
            workingDirPath,
            Credentials,
            EnvironmentVariables,
            Validation,
            StandardInputPipe,
            StandardOutputPipe,
            StandardErrorPipe
        );

        /// <summary>
        /// Creates a copy of this command, setting the credentials to the specified value.
        /// </summary>
        public Command WithCredentials(Credentials credentials) => new(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            credentials,
            EnvironmentVariables,
            Validation,
            StandardInputPipe,
            StandardOutputPipe,
            StandardErrorPipe
        );

        /// <summary>
        /// Creates a copy of this command, setting the credentials to the value configured by the specified delegate.
        /// </summary>
        public Command WithCredentials(Action<CredentialsBuilder> configure)
        {
            var builder = new CredentialsBuilder();
            configure(builder);

            return WithCredentials(builder.Build());
        }

        /// <summary>
        /// Creates a copy of this command, setting the environment variables to the specified value.
        /// </summary>
        public Command WithEnvironmentVariables(IReadOnlyDictionary<string, string> environmentVariables) => new(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            Credentials,
            environmentVariables,
            Validation,
            StandardInputPipe,
            StandardOutputPipe,
            StandardErrorPipe
        );

        /// <summary>
        /// Creates a copy of this command, setting the environment variables to the value configured by the specified delegate.
        /// </summary>
        public Command WithEnvironmentVariables(Action<EnvironmentVariablesBuilder> configure)
        {
            var builder = new EnvironmentVariablesBuilder();
            configure(builder);

            return WithEnvironmentVariables(builder.Build());
        }

        /// <summary>
        /// Creates a copy of this command, setting the validation options to the specified value.
        /// </summary>
        public Command WithValidation(CommandResultValidation validation) => new(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            Credentials,
            EnvironmentVariables,
            validation,
            StandardInputPipe,
            StandardOutputPipe,
            StandardErrorPipe
        );

        /// <summary>
        /// Creates a copy of this command, setting the standard input pipe to the specified source.
        /// </summary>
        public Command WithStandardInputPipe(PipeSource source) => new(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            Credentials,
            EnvironmentVariables,
            Validation,
            source,
            StandardOutputPipe,
            StandardErrorPipe
        );

        /// <summary>
        /// Creates a copy of this command, setting the standard output pipe to the specified target.
        /// </summary>
        public Command WithStandardOutputPipe(PipeTarget target) => new(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            Credentials,
            EnvironmentVariables,
            Validation,
            StandardInputPipe,
            target,
            StandardErrorPipe
        );

        /// <summary>
        /// Creates a copy of this command, setting the standard error pipe to the specified target.
        /// </summary>
        public Command WithStandardErrorPipe(PipeTarget target) => new(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            Credentials,
            EnvironmentVariables,
            Validation,
            StandardInputPipe,
            StandardOutputPipe,
            target
        );

        private ProcessStartInfo GetStartInfo()
        {
            var result = new ProcessStartInfo
            {
                FileName = TargetFilePath,
                Arguments = Arguments,
                WorkingDirectory = WorkingDirPath,
                UserName = Credentials.UserName,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            // Domain and password are only supported on Windows
            if (Credentials.Domain is not null || Credentials.Password is not null)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    result.Domain = Credentials.Domain;
                    result.Password = Credentials.Password?.ToSecureString();
                }
                else
                {
                    throw new NotSupportedException(
                        "Starting a process using a custom domain and password is only supported on Windows."
                    );
                }
            }

            foreach (var (key, value) in EnvironmentVariables)
                result.Environment[key] = value;

            return result;
        }

        private async Task PipeStandardInputAsync(ProcessEx process, CancellationToken cancellationToken = default)
        {
            try
            {
                // Some streams do not support cancellation, so we add a fallback that
                // drops the task and returns early.
                // Doing so does leave the original piping task still alive, which is
                // unfortunate, but still better than having everything freeze up.
                // This is important with stdin because the process might finish before
                // the pipe completes, and in case with infinite input stream it would
                // normally result in a deadlock.
                await StandardInputPipe.CopyToAsync(process.StdIn, cancellationToken)
                    .WithDangerousCancellation(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (IOException)
            {
                // IOException: The pipe has been ended.
                // This may happen if the process terminated before the pipe could complete.
                // It's not an exceptional situation because the process may not need
                // the entire stdin to complete successfully.
            }
            finally
            {
                await process.StdIn.DisposeAsync().ConfigureAwait(false);
            }
        }

        private async Task PipeStandardOutputAsync(ProcessEx process, CancellationToken cancellationToken = default)
        {
            try
            {
                await StandardOutputPipe.CopyFromAsync(process.StdOut, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                await process.StdOut.DisposeAsync().ConfigureAwait(false);
            }
        }

        private async Task PipeStandardErrorAsync(ProcessEx process, CancellationToken cancellationToken = default)
        {
            try
            {
                await StandardErrorPipe.CopyFromAsync(process.StdErr, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                await process.StdErr.DisposeAsync().ConfigureAwait(false);
            }
        }

        private async Task<CommandResult> ExecuteAsync(ProcessEx process, CancellationToken cancellationToken = default)
        {
            // Additional cancellation for stdin in case the process terminates early and doesn't fully exhaust it
            using var stdInCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Setup and start process
            using var _1 = process;
            process.Start();
            using var _2 = cancellationToken.Register(process.Kill);

            // Start piping in parallel
            var pipingTask = Task.WhenAll(
                PipeStandardInputAsync(process, stdInCts.Token),
                PipeStandardOutputAsync(process, cancellationToken),
                PipeStandardErrorAsync(process, cancellationToken)
            );

            // Wait until the process terminates or gets killed
            await process.WaitUntilExitAsync().ConfigureAwait(false);

            // Cancel stdin in case the process terminated early and doesn't need it anymore
            stdInCts.Cancel();

            try
            {
                // Wait until piping is done and propagate exceptions
                await pipingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Don't throw if cancellation happened internally and not by user request
            }

            // Validate exit code if required
            if (process.ExitCode != 0 && Validation.IsZeroExitCodeValidationEnabled())
            {
                throw CommandExecutionException.ExitCodeValidation(
                    this,
                    process.ExitCode
                );
            }

            return new CommandResult(
                process.ExitCode,
                process.StartTime,
                process.ExitTime
            );
        }

        /// <summary>
        /// Executes the command asynchronously.
        /// This method can be awaited.
        /// </summary>
        public CommandTask<CommandResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var process = new ProcessEx(GetStartInfo());
            var task = ExecuteAsync(process, cancellationToken);

            return new CommandTask<CommandResult>(task, process.Id);
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override string ToString() => $"{TargetFilePath} {Arguments}";
    }

    // Pipe operators
    public partial class Command
    {
        /// <summary>
        /// Creates a new command that pipes its standard output to the specified target.
        /// </summary>
        public static Command operator |(Command source, PipeTarget target) =>
            source.WithStandardOutputPipe(target);

        /// <summary>
        /// Creates a new command that pipes its standard output to the specified stream.
        /// </summary>
        public static Command operator |(Command source, Stream target) =>
            source | PipeTarget.ToStream(target);

        /// <summary>
        /// Creates a new command that pipes its standard output to the specified string builder.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        public static Command operator |(Command source, StringBuilder target) =>
            source | PipeTarget.ToStringBuilder(target);

        /// <summary>
        /// Creates a new command that pipes its standard output line-by-line to the specified delegate.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        public static Command operator |(Command source, Action<string> target) =>
            source | PipeTarget.ToDelegate(target);

        /// <summary>
        /// Creates a new command that pipes its standard output line-by-line to the specified delegate.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        public static Command operator |(Command source, Func<string, Task> target) =>
            source | PipeTarget.ToDelegate(target);

        /// <summary>
        /// Creates a new command that pipes its standard output and standard error to the specified targets.
        /// </summary>
        public static Command operator |(Command source, ValueTuple<PipeTarget, PipeTarget> target) =>
            source
                .WithStandardOutputPipe(target.Item1)
                .WithStandardErrorPipe(target.Item2);

        /// <summary>
        /// Creates a new command that pipes its standard output and standard error to the specified streams.
        /// </summary>
        public static Command operator |(Command source, ValueTuple<Stream, Stream> target) =>
            source | (PipeTarget.ToStream(target.Item1), PipeTarget.ToStream(target.Item2));

        /// <summary>
        /// Creates a new command that pipes its standard output and standard error to the specified string builders.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        public static Command operator |(Command source, ValueTuple<StringBuilder, StringBuilder> target) =>
            source | (PipeTarget.ToStringBuilder(target.Item1), PipeTarget.ToStringBuilder(target.Item2));

        /// <summary>
        /// Creates a new command that pipes its standard output and standard error line-by-line to the specified delegates.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the strings from byte streams.
        /// </summary>
        public static Command operator |(Command source, ValueTuple<Action<string>, Action<string>> target) =>
            source | (PipeTarget.ToDelegate(target.Item1), PipeTarget.ToDelegate(target.Item2));

        /// <summary>
        /// Creates a new command that pipes its standard output and standard error line-by-line to the specified delegates.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the strings from byte streams.
        /// </summary>
        public static Command operator |(Command source, ValueTuple<Func<string, Task>, Func<string, Task>> target) =>
            source | (PipeTarget.ToDelegate(target.Item1), PipeTarget.ToDelegate(target.Item2));

        /// <summary>
        /// Creates a new command that pipes its standard input from the specified source.
        /// </summary>
        public static Command operator |(PipeSource source, Command target) =>
            target.WithStandardInputPipe(source);

        /// <summary>
        /// Creates a new command that pipes its standard input from the specified stream.
        /// </summary>
        public static Command operator |(Stream source, Command target) =>
            PipeSource.FromStream(source) | target;

        /// <summary>
        /// Creates a new command that pipes its standard input from the specified string.
        /// Uses <see cref="Console.InputEncoding"/> to encode the string into byte stream.
        /// </summary>
        public static Command operator |(string source, Command target) =>
            PipeSource.FromString(source) | target;

        /// <summary>
        /// Creates a new command that pipes its standard input from the standard output of the specified other command.
        /// </summary>
        public static Command operator |(Command source, Command target) =>
            PipeSource.FromCommand(source) | target;
    }
}