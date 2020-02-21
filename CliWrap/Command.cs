using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Builders;
using CliWrap.Exceptions;
using CliWrap.Internal;

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
            IReadOnlyDictionary<string, string> environmentVariables,
            CommandResultValidation validation,
            PipeSource standardInputPipe,
            PipeTarget standardOutputPipe,
            PipeTarget standardErrorPipe)
        {
            TargetFilePath = targetFilePath;
            Arguments = arguments;
            WorkingDirPath = workingDirPath;
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
            "",
            Directory.GetCurrentDirectory(),
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
            EnvironmentVariables,
            Validation,
            StandardInputPipe,
            StandardOutputPipe,
            StandardErrorPipe);

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
        /// Creates a copy of this command, settings the working directory path to the specified value.
        /// </summary>
        public Command WithWorkingDirectory(string workingDirPath) => new Command(
            TargetFilePath,
            Arguments,
            workingDirPath,
            EnvironmentVariables,
            Validation,
            StandardInputPipe,
            StandardOutputPipe,
            StandardErrorPipe);

        /// <summary>
        /// Creates a copy of this command, setting the environment variables to the specified value.
        /// </summary>
        public Command WithEnvironmentVariables(IReadOnlyDictionary<string, string> environmentVariables) => new Command(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            environmentVariables,
            Validation,
            StandardInputPipe,
            StandardOutputPipe,
            StandardErrorPipe);

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
            EnvironmentVariables,
            validation,
            StandardInputPipe,
            StandardOutputPipe,
            StandardErrorPipe);

        /// <summary>
        /// Creates a copy of this command, setting the standard input pipe to the specified source.
        /// </summary>
        public Command WithStandardInputPipe(PipeSource source) => new Command(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            EnvironmentVariables,
            Validation,
            source,
            StandardOutputPipe,
            StandardErrorPipe);

        /// <summary>
        /// Creates a copy of this command, setting the standard output pipe to the specified target.
        /// </summary>
        public Command WithStandardOutputPipe(PipeTarget target) => new Command(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            EnvironmentVariables,
            Validation,
            StandardInputPipe,
            target,
            StandardErrorPipe);

        /// <summary>
        /// Creates a copy of this command, setting the standard error pipe to the specified target.
        /// </summary>
        public Command WithStandardErrorPipe(PipeTarget target) => new Command(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            EnvironmentVariables,
            Validation,
            StandardInputPipe,
            StandardOutputPipe,
            target);

        private ProcessStartInfo GetStartInfo()
        {
            var result = new ProcessStartInfo
            {
                FileName = TargetFilePath,
                WorkingDirectory = WorkingDirPath,
                Arguments = Arguments,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            foreach (var variable in EnvironmentVariables)
                result.Environment[variable.Key] = variable.Value;

            return result;
        }

        private async Task<CommandResult> ExecuteAsync(ProcessEx process, CancellationToken cancellationToken = default)
        {
            using var _ = process;

            process.Start();

            // Register cancellation
            using var cancellation = cancellationToken.Register(() => process.TryKill());

            // Handle stdin pipe
            using (process.StdIn)
                await StandardInputPipe.CopyToAsync(process.StdIn, cancellationToken);

            // Handle stdout/stderr pipes and wait for exit
            await Task.WhenAll(
                StandardOutputPipe.CopyFromAsync(process.StdOut, cancellationToken),
                StandardErrorPipe.CopyFromAsync(process.StdErr, cancellationToken),
                process.WaitUntilExitAsync());

            if (Validation.IsZeroExitCodeValidationEnabled() && process.ExitCode != 0)
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
        /// Creates a new command that pipes its standard output line-by-line to the specified delegate.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        public static Command operator |(Command source, Action<string> target) =>
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
            source
                .WithStandardOutputPipe(PipeTarget.ToStream(target.Item1))
                .WithStandardErrorPipe(PipeTarget.ToStream(target.Item2));

        /// <summary>
        /// Creates a new command that pipes its standard output and standard error line-by-line to the specified delegates.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the strings from byte streams.
        /// </summary>
        public static Command operator |(Command source, ValueTuple<Action<string>, Action<string>> target) =>
            source
                .WithStandardOutputPipe(PipeTarget.ToDelegate(target.Item1))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(target.Item2));

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