using System;
using System.Text;
using System.Threading;

namespace CliWrap.Buffered;

/// <summary>
/// Buffered execution model.
/// </summary>
public static class BufferedCommandExtensions
{
    /// <summary>
    /// Executes the command asynchronously.
    /// The result of this execution contains the standard output and standard error streams buffered in-memory as strings.
    /// </summary>
    /// <remarks>
    /// This method can be awaited.
    /// </remarks>
    public static CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
        this Command command,
        Encoding standardOutputEncoding,
        Encoding standardErrorEncoding,
        CancellationToken cancellationToken = default)
    {
        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();

        var stdOutPipe = Pipe.ToMany(
            command.StandardOutputPipe,
            Pipe.ToStringBuilder(stdOutBuffer, standardOutputEncoding)
        );

        var stdErrPipe = Pipe.ToMany(
            command.StandardErrorPipe,
            Pipe.ToStringBuilder(stdErrBuffer, standardErrorEncoding)
        );

        var commandPiped = command
            .WithStandardOutputPipe(stdOutPipe)
            .WithStandardErrorPipe(stdErrPipe)
            .WithValidation(CommandResultValidation.None); // disable validation because we have our own

        return commandPiped
            .ExecuteAsync(cancellationToken)
            .Select(r =>
            {
                // Transform the result
                var result = new BufferedCommandResult(
                    r.ExitCode,
                    r.StartTime,
                    r.ExitTime,
                    stdOutBuffer.ToString(),
                    stdErrBuffer.ToString()
                );

                // We perform validation separately here because we want to include stderr in the exception as well
                if (result.ExitCode != 0 && command.Validation.IsZeroExitCodeValidationEnabled())
                {
                    throw CommandExecutionException.ValidationError(
                        command,
                        result.ExitCode,
                        result.StandardError.Trim()
                    );
                }

                return result;
            });
    }

    /// <summary>
    /// Executes the command asynchronously.
    /// The result of this execution contains the standard output and standard error streams buffered in-memory as strings.
    /// </summary>
    /// <remarks>
    /// This method can be awaited.
    /// </remarks>
    public static CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
        this Command command,
        Encoding encoding,
        CancellationToken cancellationToken = default) =>
        command.ExecuteBufferedAsync(encoding, encoding, cancellationToken);

    /// <summary>
    /// Executes the command asynchronously.
    /// The result of this execution contains the standard output and standard error streams buffered in-memory as strings.
    /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
    /// </summary>
    /// <remarks>
    /// This method can be awaited.
    /// </remarks>
    public static CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
        this Command command,
        CancellationToken cancellationToken = default) =>
        command.ExecuteBufferedAsync(Console.OutputEncoding, cancellationToken);
}