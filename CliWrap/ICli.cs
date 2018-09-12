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
        Cli SetWorkingDirectory(string workingDirectory);

        /// <summary>
        /// Sets the command line arguments.
        /// </summary>
        Cli SetArguments(string arguments);

        /// <summary>
        /// Sets the standard input.
        /// </summary>
        Cli SetStandardInput(Stream standardInput);

        /// <summary>
        /// Sets the standard input to given string using given encoding.
        /// </summary>
        Cli SetStandardInput(string standardInput, Encoding encoding);

        /// <summary>
        /// Sets the standard input to given string.
        /// </summary>
        Cli SetStandardInput(string standardInput);

        /// <summary>
        /// Sets an environment variable to the given value.
        /// Can be called more than once to set multiple environment variables.
        /// </summary>
        Cli SetEnvironmentVariable(string key, string value);

        /// <summary>
        /// Sets the text encoding used for standard output stream.
        /// </summary>
        Cli SetStandardOutputEncoding(Encoding standardOutputEncoding);

        /// <summary>
        /// Sets the text encoding used for standard error stream.
        /// </summary>
        Cli SetStandardErrorEncoding(Encoding standardErrorEncoding);

        /// <summary>
        /// Sets the delegate that will be called whenever a new line is appended to standard output stream.
        /// </summary>
        Cli SetStandardOutputCallback(Action<string> callback);

        /// <summary>
        /// Sets the delegate that will be called whenever a new line is appended to standard error stream.
        /// </summary>
        Cli SetStandardErrorCallback(Action<string> callback);

        /// <summary>
        /// Sets the cancellation token.
        /// </summary>
        Cli SetCancellationToken(CancellationToken cancellationToken);

        /// <summary>
        /// Enables or disables validation that will throw <see cref="ExitCodeValidationException"/> if the resulting exit code is not zero.
        /// </summary>
        Cli EnableExitCodeValidation(bool isEnabled = true);

        /// <summary>
        /// Enables or disables validation that will throw <see cref="StandardErrorValidationException"/> if the resulting standard error is not empty.
        /// </summary>
        Cli EnableStandardErrorValidation(bool isEnabled = true);

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