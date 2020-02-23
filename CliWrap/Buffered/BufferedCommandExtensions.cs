using System;
using System.Text;
using System.Threading;

namespace CliWrap.Buffered
{
    /// <summary>
    /// Convenience extension for executing a command while buffering its streams.
    /// </summary>
    public static class BufferedCommandExtensions
    {
        /// <summary>
        /// Executes the command asynchronously.
        /// The result of this execution contains the standard output and standard error streams buffered in-memory as strings.
        /// This method can be awaited.
        /// </summary>
        public static CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
            this Command command,
            Encoding standardOutputEncoding,
            Encoding standardErrorEncoding,
            CancellationToken cancellationToken = default)
        {
            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();

            var stdOutPipe = PipeTarget.Merge(command.StandardOutputPipe,
                PipeTarget.ToStringBuilder(stdOutBuffer, standardOutputEncoding));
            var stdErrPipe = PipeTarget.Merge(command.StandardErrorPipe,
                PipeTarget.ToStringBuilder(stdErrBuffer, standardErrorEncoding));

            var commandPiped = command
                .WithStandardOutputPipe(stdOutPipe)
                .WithStandardErrorPipe(stdErrPipe);

            return commandPiped
                .ExecuteAsync(cancellationToken)
                .Select(r => new BufferedCommandResult(
                    r.ExitCode,
                    r.StartTime,
                    r.ExitTime,
                    stdOutBuffer.ToString(),
                    stdErrBuffer.ToString()));
        }

        /// <summary>
        /// Executes the command asynchronously.
        /// The result of this execution contains the standard output and standard error streams buffered in-memory as strings.
        /// This method can be awaited.
        /// </summary>
        public static CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
            this Command command,
            Encoding encoding,
            CancellationToken cancellationToken = default) =>
            command.ExecuteBufferedAsync(encoding, encoding, cancellationToken);

        /// <summary>
        /// Executes the command asynchronously.
        /// The result of this execution contains the standard output and standard error streams buffered in-memory as strings.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the strings from byte streams.
        /// This method can be awaited.
        /// </summary>
        public static CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
            this Command command,
            CancellationToken cancellationToken = default) =>
            command.ExecuteBufferedAsync(Console.OutputEncoding, cancellationToken);
    }
}