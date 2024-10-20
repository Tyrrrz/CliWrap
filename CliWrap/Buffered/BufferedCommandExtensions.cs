using System;
using System.Text;
using System.Threading;
using CliWrap.Exceptions;

namespace CliWrap.Buffered;

/// <summary>
/// Buffered execution model.
/// </summary>
public static class BufferedCommandExtensions
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
    public static CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
        this Command command,
        Encoding standardOutputEncoding,
        Encoding standardErrorEncoding,
        CancellationToken forcefulCancellationToken,
        CancellationToken gracefulCancellationToken
    )
    {
        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();

        var stdOutPipe = PipeTarget.Merge(
            command.StandardOutputPipe,
            PipeTarget.ToStringBuilder(stdOutBuffer, standardOutputEncoding)
        );

        var stdErrPipe = PipeTarget.Merge(
            command.StandardErrorPipe,
            PipeTarget.ToStringBuilder(stdErrBuffer, standardErrorEncoding)
        );

        var commandWithPipes = command
            .WithStandardOutputPipe(stdOutPipe)
            .WithStandardErrorPipe(stdErrPipe);

        return commandWithPipes
            .ExecuteAsync(forcefulCancellationToken, gracefulCancellationToken)
            .Bind(async task =>
            {
                try
                {
                    var result = await task;

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

    /// <summary>
    /// Executes the command asynchronously with buffering.
    /// Data written to the standard output and standard error streams is decoded as text
    /// and returned as part of the result object.
    /// </summary>
    /// <remarks>
    /// This method can be awaited.
    /// </remarks>
    public static CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
        this Command command,
        Encoding standardOutputEncoding,
        Encoding standardErrorEncoding,
        CancellationToken cancellationToken = default
    )
    {
        return command.ExecuteBufferedAsync(
            standardOutputEncoding,
            standardErrorEncoding,
            cancellationToken,
            CancellationToken.None
        );
    }

    /// <summary>
    /// Executes the command asynchronously with buffering.
    /// Data written to the standard output and standard error streams is decoded as text
    /// and returned as part of the result object.
    /// </summary>
    /// <remarks>
    /// This method can be awaited.
    /// </remarks>
    public static CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
        this Command command,
        Encoding encoding,
        CancellationToken cancellationToken = default
    )
    {
        return command.ExecuteBufferedAsync(encoding, encoding, cancellationToken);
    }

    /// <summary>
    /// Executes the command asynchronously with buffering.
    /// Data written to the standard output and standard error streams is decoded as text
    /// and returned as part of the result object.
    /// Uses <see cref="Encoding.Default" /> for decoding.
    /// </summary>
    /// <remarks>
    /// This method can be awaited.
    /// </remarks>
    public static CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
        this Command command,
        CancellationToken cancellationToken = default
    )
    {
        return command.ExecuteBufferedAsync(Encoding.Default, cancellationToken);
    }
}
