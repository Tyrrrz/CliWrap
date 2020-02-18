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
        private readonly string _targetFilePath;

        private string _workingDirPath = Directory.GetCurrentDirectory();
        private string _arguments = "";

        private PipeSource _stdInPipe = PipeSource.Null;
        private PipeTarget _stdOutPipe = PipeTarget.Null;
        private PipeTarget _stdErrPipe = PipeTarget.Null;



        private IReadOnlyDictionary<string, string> _env = new Dictionary<string, string>();
        private Encoding _stdOutEncoding = Console.OutputEncoding;
        private Encoding _stdErrEncoding = Console.OutputEncoding;
        private bool _isExitCodeValidationEnabled = true;

        public Cli(string targetFilePath)
        {
            _targetFilePath = targetFilePath;
        }

        public Cli SetWorkingDirectory(string path)
        {
            _workingDirPath = path;
            return this;
        }

        public Cli SetArguments(string arguments)
        {
            _arguments = arguments;
            return this;
        }

        public Cli SetArguments(Action<CliArgumentBuilder> configure)
        {
            var builder = new CliArgumentBuilder();
            configure(builder);

            return SetArguments(builder.Build());
        }

        public Cli PipeStandardInput(PipeSource source)
        {
            _stdInPipe = source;
            return this;
        }

        public Cli PipeStandardOutput(PipeTarget target)
        {
            _stdOutPipe = target;
            return this;
        }

        public Cli PipeStandardError(PipeTarget target)
        {
            _stdErrPipe = target;
            return this;
        }

        public Cli SetEnvironmentVariables(IReadOnlyDictionary<string, string> env)
        {
            _env = env;
            return this;
        }

        public Cli SetEnvironmentVariables(Action<IDictionary<string, string>> configure)
        {
            var variables = new Dictionary<string, string>(StringComparer.Ordinal);
            configure(variables);

            return SetEnvironmentVariables(variables);
        }

        public Cli SetStandardOutputEncoding(Encoding encoding)
        {
            _stdOutEncoding = encoding;
            return this;
        }

        public Cli SetStandardErrorEncoding(Encoding encoding)
        {
            _stdErrEncoding = encoding;
            return this;
        }

        public Cli EnableExitCodeValidation(bool isEnabled = true)
        {
            _isExitCodeValidationEnabled = isEnabled;
            return this;
        }

        private ProcessStartInfo GetStartInfo()
        {
            var result = new ProcessStartInfo
            {
                FileName = _targetFilePath,
                WorkingDirectory = _workingDirPath,
                Arguments = _arguments,
                StandardOutputEncoding = _stdOutEncoding,
                StandardErrorEncoding = _stdErrEncoding,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            foreach (var variable in _env)
                result.Environment[variable.Key] = variable.Value;

            return result;
        }

        private async Task<CliResult> ExecuteAsync(ProcessEx process, CancellationToken cancellationToken = default)
        {
            using var _ = process;

            process.Start();

            using (process.StdIn)
                await _stdInPipe.CopyToAsync(process.StdIn.BaseStream, cancellationToken);

            await Task.WhenAll(
                _stdOutPipe.CopyFromAsync(process.StdOut.BaseStream, cancellationToken),
                _stdErrPipe.CopyFromAsync(process.StdErr.BaseStream, cancellationToken),
                process.WaitUntilExitAsync(cancellationToken));

            if (_isExitCodeValidationEnabled && process.ExitCode != 0)
                throw CliExecutionException.ExitCodeValidation(_targetFilePath, _arguments, process.ExitCode);

            return new CliResult(process.ExitCode, process.StartTime, process.ExitTime);
        }

        public ProcessTask<CliResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var process = new ProcessEx(GetStartInfo());
            var task = ExecuteAsync(process, cancellationToken);

            return new ProcessTask<CliResult>(task, process.Id);
        }

        private async Task<BufferedCliResult> ExecuteBufferedAsync(ProcessEx process, CancellationToken cancellationToken = default)
        {
            var stdOutBuffer = new MemoryStream();
            var stdErrBuffer = new MemoryStream();

            // TODO: this shouldn't happen on every execute
            _stdOutPipe = PipeTarget.Merge(_stdOutPipe, PipeTarget.FromStream(stdOutBuffer));
            _stdErrPipe = PipeTarget.Merge(_stdErrPipe, PipeTarget.FromStream(stdErrBuffer));

            var result = await ExecuteAsync(process, cancellationToken);

            var stdOut = _stdOutEncoding.GetString(stdOutBuffer.ToArray());
            var stdErr = _stdErrEncoding.GetString(stdErrBuffer.ToArray());

            return new BufferedCliResult(result.ExitCode, result.StartTime, result.ExitTime, stdOut, stdErr);
        }

        public ProcessTask<BufferedCliResult> ExecuteBufferedAsync(CancellationToken cancellationToken = default)
        {
            var process = new ProcessEx(GetStartInfo());
            var task = ExecuteBufferedAsync(process, cancellationToken);

            return new ProcessTask<BufferedCliResult>(task, process.Id);
        }

        public override string ToString() => $"{_targetFilePath} {_arguments}";
    }

    public partial class Cli
    {
        public static Cli operator |(Cli source, PipeTarget target) =>
            source.PipeStandardOutput(target);

        public static Cli operator |(Cli source, Stream target) =>
            source | PipeTarget.FromStream(target);

        public static Cli operator |(Cli source, ValueTuple<PipeTarget, PipeTarget> target) =>
            source.PipeStandardOutput(target.Item1).PipeStandardError(target.Item2);

        public static Cli operator |(Cli source, ValueTuple<Stream, Stream> target) =>
            source.PipeStandardOutput(PipeTarget.FromStream(target.Item1)).PipeStandardError(PipeTarget.FromStream(target.Item2));

        public static Cli operator |(PipeSource source, Cli target) =>
            target.PipeStandardInput(source);

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