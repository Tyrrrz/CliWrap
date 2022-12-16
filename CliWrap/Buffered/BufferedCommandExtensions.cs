﻿using System;
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
    /// Executes the command asynchronously.
    /// The result of this execution contains the standard output and standard error streams
    /// buffered in-memory as strings.
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
        CancellationToken gracefulCancellationToken)
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
            .WithStandardErrorPipe(stdErrPipe)
            // Disable validation because we have our own
            .WithValidation(CommandResultValidation.None);

        return commandWithPipes
            .ExecuteAsync(forcefulCancellationToken, gracefulCancellationToken)
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

                // Perform validation separately here because we want to include stderr in the exception as well
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
    /// The result of this execution contains the standard output and standard error streams
    /// buffered in-memory as strings.
    /// </summary>
    /// <remarks>
    /// This method can be awaited.
    /// </remarks>
    public static CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
        this Command command,
        Encoding standardOutputEncoding,
        Encoding standardErrorEncoding,
        CancellationToken cancellationToken = default) =>
        command.ExecuteBufferedAsync(
            standardOutputEncoding,
            standardErrorEncoding,
            cancellationToken,
            CancellationToken.None
        );

    /// <summary>
    /// Executes the command asynchronously.
    /// The result of this execution contains the standard output and standard error streams
    /// buffered in-memory as strings.
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
    /// The result of this execution contains the standard output and standard error streams
    /// buffered in-memory as strings.
    /// Uses <see cref="Console.OutputEncoding" /> to decode byte streams.
    /// </summary>
    /// <remarks>
    /// This method can be awaited.
    /// </remarks>
    public static CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
        this Command command,
        CancellationToken cancellationToken = default) =>
        command.ExecuteBufferedAsync(Console.OutputEncoding, cancellationToken);
}