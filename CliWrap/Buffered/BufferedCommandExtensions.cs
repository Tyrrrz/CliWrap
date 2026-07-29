using System.Text;
using System.Threading;
using CliWrap.Exceptions;

namespace CliWrap.Buffered;

/// <summary>
/// Buffered execution model.
/// </summary>
public static class BufferedCommandExtensions
{
    /// <inheritdoc cref="BufferedCommandExtensions" />
    extension(Command command)
    {
        /// <summary>
        /// Executes the command asynchronously with buffering.
        /// Data written to the standard output and standard error streams is decoded as text
        /// and returned as part of the result object.
        /// </summary>
        /// <remarks>
        /// This method can be awaited.
        /// </remarks>
        // TODO: (breaking change) use optional parameters and remove the other overload
        public CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
            Encoding standardOutputEncoding,
            Encoding standardErrorEncoding,
            CancellationToken forcefulCancellationToken,
            CancellationToken gracefulCancellationToken
        )
        {
            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();

            return command
                // Extend the existing standard output pipe to also write data to a buffer
                .WithStandardOutputPipe(
                    PipeTarget.Merge(
                        command.StandardOutputPipe,
                        PipeTarget.ToStringBuilder(stdOutBuffer, standardOutputEncoding)
                    )
                )
                // Extend the existing standard error pipe to also write data to a buffer
                .WithStandardErrorPipe(
                    PipeTarget.Merge(
                        command.StandardErrorPipe,
                        PipeTarget.ToStringBuilder(stdErrBuffer, standardErrorEncoding)
                    )
                )
                .ExecuteAsync(forcefulCancellationToken, gracefulCancellationToken)
                // CommandTask<> doesn't have a method builder, so we wrap it manually to
                // transform the result into an object that also includes the contents of
                // the standard output and standard error buffers.
                .Wrap(async task =>
                {
                    try
                    {
                        var result = await task.ConfigureAwait(false);

                        return new BufferedCommandResult(
                            result.ExitCode,
                            result.StartTime,
                            result.ExitTime,
                            stdOutBuffer.ToString(),
                            stdErrBuffer.ToString()
                        );
                    }
                    catch (CommandExecutionException ex)
                    {
                        // In case of a command exception (i.e., non-zero exit code), we can also include the
                        // standard error output in the exception for better diagnostics.
                        throw new CommandExecutionException(
                            ex.Command,
                            ex.ExitCode,
                            $"""
                            Command execution failed, see the inner exception for details.

                            Standard error:
                            {stdErrBuffer.ToString().Trim()}
                            """,
                            ex
                        );
                    }
                });
        }

        /// <inheritdoc cref="ExecuteBufferedAsync(Command, Encoding, Encoding, CancellationToken, CancellationToken)" />
        public CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
            Encoding standardOutputEncoding,
            Encoding standardErrorEncoding,
            CancellationToken cancellationToken = default
        ) =>
            command.ExecuteBufferedAsync(
                standardOutputEncoding,
                standardErrorEncoding,
                cancellationToken,
                CancellationToken.None
            );

        /// <inheritdoc cref="ExecuteBufferedAsync(Command, Encoding, Encoding, CancellationToken, CancellationToken)" />
        public CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
            Encoding encoding,
            CancellationToken cancellationToken = default
        ) => command.ExecuteBufferedAsync(encoding, encoding, cancellationToken);

        /// <inheritdoc cref="ExecuteBufferedAsync(Command, Encoding, Encoding, CancellationToken, CancellationToken)" />
        public CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
            CancellationToken cancellationToken = default
        ) => command.ExecuteBufferedAsync(Encoding.Default, cancellationToken);
    }
}
