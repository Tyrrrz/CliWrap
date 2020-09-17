using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Builders;
using CliWrap.Exceptions;
using CliWrap.Internal;
using CliWrap.Internal.Extensions;

namespace CliWrap
{
    /// <summary>
    /// Represents a shell command.
    /// </summary>
    public partial class Command
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
        public Command WithArguments(string arguments) => new Command(
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
        // TODO: replace with optional argument when breaking changes are ok
        public Command WithArguments(IEnumerable<string> arguments) =>
            WithArguments(arguments, true);

        /// <summary>
        /// Creates a copy of this command, settings the working directory path to the specified value.
        /// </summary>
        public Command WithWorkingDirectory(string workingDirPath) => new Command(
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
        public Command WithCredentials(Credentials credentials) => new Command(
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
        public Command WithEnvironmentVariables(IReadOnlyDictionary<string, string> environmentVariables) => new Command(
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
        public Command WithValidation(CommandResultValidation validation) => new Command(
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
        public Command WithStandardInputPipe(PipeSource source) => new Command(
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
        public Command WithStandardOutputPipe(PipeTarget target) => new Command(
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
        public Command WithStandardErrorPipe(PipeTarget target) => new Command(
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
            // Secure string should not be used for new development:
            // https://github.com/dotnet/platform-compat/blob/master/docs/DE0001.md
            var securePassword = Credentials.Password?.ToSecureString();

            var result = new ProcessStartInfo
            {
                FileName = TargetFilePath,
                Arguments = Arguments,
                WorkingDirectory = WorkingDirPath,
                Domain = Credentials.Domain,
                UserName = Credentials.UserName,
                Password = securePassword,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            foreach (var (key, value) in EnvironmentVariables)
                result.Environment[key] = value;

            return result;
        }

        private async Task<CommandResult> ExecuteAsync(ProcessEx process, CancellationToken cancellationToken = default)
        {
            using var _ = process;
            process.Start();

            // Stdin pipe may need to be canceled early if the process terminates before it finishes
            using var stdInCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Register early process termination
            using var cancellation = cancellationToken.Register(() => process.TryKill());

            // Stdin must be closed after it finished to avoid deadlock if the process reads the stream to end
            async Task HandleStdInAsync()
            {
                try
                {
                    // Some streams don't support cancellation, in which case we need a fallback mechanism to avoid deadlocks.
                    // For example, WindowsConsoleStream (from Console.OpenStandardInput()) in particular doesn't support cancellation.
                    // In the following case the operation will terminate but a rogue Task will leak and might cause problems.
                    // This is a non-issue, however, if the user closes the stream at the earliest opportunity.
                    // Otherwise we enter an indeterminate state and tell ourselves we did everything we could to avoid it.
                    await Task.WhenAny(
                        StandardInputPipe.CopyToAsync(process.StdIn, stdInCts.Token),
                        Task.Delay(-1, stdInCts.Token)
                    );
                }
                // Ignore cancellation here, will propagate later
                catch (OperationCanceledException)
                {
                }
                // We want to ignore I/O exceptions that happen when the output stream has already closed.
                // This can happen when the process reads only a portion of stdin and then exits.
                // Unfortunately we can't catch a specific exception for this exact event so we have no choice but to catch all of them.
                catch (IOException)
                {
                }
                finally
                {
                    await process.StdIn.DisposeAsync();
                }
            }

            // Stdout doesn't need to be closed but we do it for good measure
            async Task HandleStdOutAsync()
            {
                try
                {
                    await StandardOutputPipe.CopyFromAsync(process.StdOut, cancellationToken);
                }
                // Ignore cancellation here, will propagate later
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    await process.StdOut.DisposeAsync();
                }
            }

            // Stderr doesn't need to be closed but we do it for good measure
            async Task HandleStdErrAsync()
            {
                try
                {
                    await StandardErrorPipe.CopyFromAsync(process.StdErr, cancellationToken);
                }
                // Ignore cancellation here, will propagate later
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    await process.StdErr.DisposeAsync();
                }
            }

            // Handle pipes in background and in parallel to avoid deadlocks
            var pipingTasks = new[]
            {
                HandleStdInAsync(),
                HandleStdOutAsync(),
                HandleStdErrAsync()
            };

            // Wait until the process terminates or gets killed
            await process.WaitUntilExitAsync();

            // Stop piping stdin if the process has already exited (can happen if not all of stdin is read)
            stdInCts.Cancel();

            // Ensure all pipes are finished
            await Task.WhenAll(pipingTasks);

            // Propagate cancellation to the user
            cancellationToken.ThrowIfCancellationRequested();

            // Validate exit code
            if (process.ExitCode != 0 && Validation.IsZeroExitCodeValidationEnabled())
                throw CommandExecutionException.ExitCodeValidation(TargetFilePath, Arguments, process.ExitCode);

            return new CommandResult(process.ExitCode, process.StartTime, process.ExitTime);
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