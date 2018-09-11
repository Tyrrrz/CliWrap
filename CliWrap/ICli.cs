using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Exceptions;
using CliWrap.Models;

namespace CliWrap
{
    /// <summary>
    /// An interface for <see cref="Cli"/>.
    /// </summary>
    public interface ICli
    {
        /// <summary>
        /// Sets the working directory.
        /// </summary>
        Cli WithWorkingDirectory(string workingDirectory);

        /// <summary>
        /// Sets the command line arguments.
        /// </summary>
        Cli WithArguments(string arguments);

        /// <summary>
        /// Sets the standard input.
        /// </summary>
        Cli WithStandardInput(Stream standardInput);

        /// <summary>
        /// Sets the standard input to given string.
        /// </summary>
        Cli WithStandardInput(string standardInput);

        /// <summary>
        /// Sets the standard input to given string using given encoding.
        /// </summary>
        Cli WithStandardInput(string standardInput, Encoding encoding);

        /// <summary>
        /// Sets an environment variable to the given value.
        /// Can be called more than once to set multiple environment variables.
        /// </summary>
        Cli WithEnvironmentVariable(string key, string value);

        /// <summary>
        /// Sets the text encoding used for standard output stream.
        /// </summary>
        Cli WithStandardOutputEncoding(Encoding standardOutputEncoding);

        /// <summary>
        /// Sets the text encoding used for standard error stream.
        /// </summary>
        Cli WithStandardErrorEncoding(Encoding standardErrorEncoding);

        /// <summary>
        /// Sets the delegate that will be called whenever a new line is appended to standard output stream.
        /// </summary>
        Cli WithStandardOutputObserver(Action<string> observer);

        /// <summary>
        /// Sets the observer that will be notified whenever a new line is appended to standard output stream.
        /// </summary>
        Cli WithStandardOutputObserver(IObserver<string> observer);

        /// <summary>
        /// Sets the delegate that will be called whenever a new line is appended to standard error stream.
        /// </summary>
        Cli WithStandardErrorObserver(Action<string> observer);

        /// <summary>
        /// Sets the observer that will be notified whenever a new line is appended to standard error stream.
        /// </summary>
        Cli WithStandardErrorObserver(IObserver<string> observer);

        /// <summary>
        /// Sets the cancellation token.
        /// </summary>
        Cli WithCancellationToken(CancellationToken cancellationToken);

        /// <summary>
        /// Enables or disables validation that will throw <see cref="ExitCodeValidationException"/> if the resulting exit code is not zero.
        /// </summary>
        Cli WithExitCodeValidation(bool isEnabled = true);

        /// <summary>
        /// Enables or disables validation that will throw <see cref="StandardErrorValidationException"/> if the resulting standard error is not empty.
        /// </summary>
        Cli WithStandardErrorValidation(bool isEnabled = true);

        /// <summary>
        /// Executes the process and waits until it exists synchronously.
        /// </summary>
        ExecutionResult Execute();

        /// <summary>
        /// Executes the process and waits for it to exit asynchronously.
        /// </summary>
        Task<ExecutionResult> ExecuteAsync();

        /// <summary>
        /// Executes the process and doesn't wait for it to exit.
        /// </summary>
        void ExecuteAndForget();
    }
}