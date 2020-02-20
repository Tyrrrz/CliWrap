using System;
using System.Text;
using System.Threading;

namespace CliWrap.Buffered
{
    /// <summary>
    /// Convenience extensions for executing a command and buffering its streams.
    /// </summary>
    public static class BufferedCommandExtensions
    {
        /// <summary>
        /// Executes the command asynchronously.
        /// Pipes the standard output and standard error streams into in-memory buffers which are contained within the result.
        /// Any existing pipes are also preserved.
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

            // Preserve the existing pipes by merging them with ours
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
        /// Pipes the standard output and standard error streams into in-memory buffers which are contained within the result.
        /// This method can be awaited.
        /// </summary>
        public static CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
            this Command command,
            Encoding encoding,
            CancellationToken cancellationToken = default) =>
            command.ExecuteBufferedAsync(encoding, encoding, cancellationToken);

        /// <summary>
        /// Executes the command asynchronously.
        /// Pipes the standard output and standard error streams into in-memory buffers which are contained within the result.
        /// This method can be awaited.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the strings from byte streams.
        /// </summary>
        public static CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
            this Command command,
            CancellationToken cancellationToken = default) =>
            command.ExecuteBufferedAsync(Console.OutputEncoding, cancellationToken);
    }
}