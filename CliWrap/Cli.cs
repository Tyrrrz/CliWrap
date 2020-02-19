using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Exceptions;
using CliWrap.Internal;

namespace CliWrap
{
    public partial class Cli
    {
        public string TargetFilePath { get; }

        public string Arguments { get; }

        public string WorkingDirPath { get; }

        public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

        public ResultValidation Validation { get; }

        public PipeSource StandardInputPipe { get; }

        public PipeTarget StandardOutputPipe { get; }

        public PipeTarget StandardErrorPipe { get; }

        public Cli(
            string targetFilePath,
            string arguments,
            string workingDirPath,
            IReadOnlyDictionary<string, string> environmentVariables,
            ResultValidation validation,
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

        public Cli(string targetFilePath) : this(
            targetFilePath,
            "",
            Directory.GetCurrentDirectory(),
            new Dictionary<string, string>(),
            ResultValidation.ZeroExitCode,
            PipeSource.Null,
            PipeTarget.Null,
            PipeTarget.Null)
        {
        }

        public Cli WithArguments(string arguments) => new Cli(
            TargetFilePath,
            arguments,
            WorkingDirPath,
            EnvironmentVariables,
            Validation,
            StandardInputPipe,
            StandardOutputPipe,
            StandardErrorPipe);

        public Cli WithArguments(Action<CliArgumentBuilder> configure)
        {
            var builder = new CliArgumentBuilder();
            configure(builder);

            return WithArguments(builder.Build());
        }

        public Cli WithWorkingDirectory(string workingDirPath) => new Cli(
            TargetFilePath,
            Arguments,
            workingDirPath,
            EnvironmentVariables,
            Validation,
            StandardInputPipe,
            StandardOutputPipe,
            StandardErrorPipe);

        public Cli WithEnvironmentVariables(IReadOnlyDictionary<string, string> environmentVariables) => new Cli(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            environmentVariables,
            Validation,
            StandardInputPipe,
            StandardOutputPipe,
            StandardErrorPipe);

        public Cli WithEnvironmentVariables(Action<IDictionary<string, string>> configure)
        {
            var variables = new Dictionary<string, string>(StringComparer.Ordinal);
            configure(variables);

            return WithEnvironmentVariables(variables);
        }

        public Cli WithValidation(ResultValidation validation) => new Cli(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            EnvironmentVariables,
            validation,
            StandardInputPipe,
            StandardOutputPipe,
            StandardErrorPipe);

        public Cli WithStandardInputPipe(PipeSource source) => new Cli(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            EnvironmentVariables,
            Validation,
            source,
            StandardOutputPipe,
            StandardErrorPipe);

        public Cli WithStandardOutputPipe(PipeTarget target) => new Cli(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            EnvironmentVariables,
            Validation,
            StandardInputPipe,
            target,
            StandardErrorPipe);

        public Cli WithStandardErrorPipe(PipeTarget target) => new Cli(
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

        private async Task<CliResult> ExecuteAsync(
            ProcessEx process,
            PipeSource stdInPipe,
            PipeTarget stdOutPipe,
            PipeTarget stdErrPipe,
            CancellationToken cancellationToken = default)
        {
            using var _ = process;

            process.Start();

            using (process.StdIn)
                await stdInPipe.CopyToAsync(process.StdIn.BaseStream, cancellationToken);

            await Task.WhenAll(
                stdOutPipe.CopyFromAsync(process.StdOut.BaseStream, cancellationToken),
                stdErrPipe.CopyFromAsync(process.StdErr.BaseStream, cancellationToken),
                process.WaitUntilExitAsync(cancellationToken));

            if (Validation.IsZeroExitCodeValidationEnabled() && process.ExitCode != 0)
                throw CliExecutionException.ExitCodeValidation(TargetFilePath, Arguments, process.ExitCode);

            return new CliResult(process.ExitCode, process.StartTime, process.ExitTime);
        }

        public ProcessTask<CliResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var process = new ProcessEx(GetStartInfo());
            var task = ExecuteAsync(process, StandardInputPipe, StandardOutputPipe, StandardErrorPipe, cancellationToken);

            return new ProcessTask<CliResult>(task, process.Id);
        }

        private async Task<BufferedCliResult> ExecuteBufferedAsync(ProcessEx process, CancellationToken cancellationToken = default)
        {
            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();

            var stdInPipe = StandardInputPipe;
            var stdOutPipe = PipeTarget.Merge(StandardOutputPipe, PipeTarget.ToStringBuilder(stdOutBuffer));
            var stdErrPipe = PipeTarget.Merge(StandardErrorPipe, PipeTarget.ToStringBuilder(stdErrBuffer));

            var result = await ExecuteAsync(process, stdInPipe, stdOutPipe, stdErrPipe, cancellationToken);

            var stdOut = stdOutBuffer.ToString();
            var stdErr = stdErrBuffer.ToString();

            return new BufferedCliResult(result.ExitCode, result.StartTime, result.ExitTime, stdOut, stdErr);
        }

        public ProcessTask<BufferedCliResult> ExecuteBufferedAsync(CancellationToken cancellationToken = default)
        {
            var process = new ProcessEx(GetStartInfo());
            var task = ExecuteBufferedAsync(process, cancellationToken);

            return new ProcessTask<BufferedCliResult>(task, process.Id);
        }

        public override string ToString() => $"{TargetFilePath} {Arguments}";
    }

    public partial class Cli
    {
        public static Cli operator |(Cli source, PipeTarget target) =>
            source.WithStandardOutputPipe(target);

        public static Cli operator |(Cli source, Stream target) =>
            source | PipeTarget.ToStream(target);

        public static Cli operator |(Cli source, ValueTuple<PipeTarget, PipeTarget> target) =>
            source
                .WithStandardOutputPipe(target.Item1)
                .WithStandardErrorPipe(target.Item2);

        public static Cli operator |(Cli source, ValueTuple<Stream, Stream> target) =>
            source
                .WithStandardOutputPipe(PipeTarget.ToStream(target.Item1))
                .WithStandardErrorPipe(PipeTarget.ToStream(target.Item2));

        public static Cli operator |(Cli source, ValueTuple<Action<string>, Action<string>> target) =>
            source
                .WithStandardOutputPipe(PipeTarget.ToDelegate(target.Item1))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(target.Item2));

        public static Cli operator |(PipeSource source, Cli target) =>
            target.WithStandardInputPipe(source);

        public static Cli operator |(Stream source, Cli target) =>
            PipeSource.FromStream(source) | target;

        public static Cli operator |(string source, Cli target) =>
            PipeSource.FromString(source) | target;

        public static Cli operator |(Cli source, Cli target) =>
            PipeSource.FromCli(source) | target;
    }

    public partial class Cli
    {
        public static Cli Wrap(string targetFilePath) => new Cli(targetFilePath);
    }
}