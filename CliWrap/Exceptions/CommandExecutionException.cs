using System;
using System.Diagnostics.CodeAnalysis;

namespace CliWrap.Exceptions;

/// <summary>
/// Exception thrown when the command fails to execute correctly.
/// </summary>
public class CommandExecutionException : CliWrapException
{
    /// <summary>
    /// Command that triggered the exception.
    /// </summary>
    public ICommandConfiguration Command { get; }

    /// <summary>
    /// Exit code returned by the process.
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    /// Initializes an instance of <see cref="CommandExecutionException" />.
    /// </summary>
    public CommandExecutionException(
        ICommandConfiguration command,
        int exitCode,
        string message,
        Exception? innerException)
        : base(message, innerException)
    {
        Command = command;
        ExitCode = exitCode;
    }

    /// <summary>
    /// Initializes an instance of <see cref="CommandExecutionException" />.
    /// </summary>
    // TODO: (breaking change) remove in favor of an optional parameter in the constructor above
    [ExcludeFromCodeCoverage]
    public CommandExecutionException(
        ICommandConfiguration command,
        int exitCode,
        string message)
        : this(command, exitCode, message, null)
    {
    }
}