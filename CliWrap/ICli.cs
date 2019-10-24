using System;
using System.Collections.Generic;
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
        /// Process ID associated with the last execution or null if the process hasn't been started yet.
        /// </summary>
        int? ProcessId { get; }

        /// <summary>
        /// Sets the working directory.
        /// </summary>
        ICli SetWorkingDirectory(string workingDirectory);

        /// <summary>
        /// Sets the command line arguments.
        /// </summary>
        ICli SetArguments(string arguments);

        /// <summary>
        /// Sets the command line arguments.
        /// </summary>
        ICli SetArguments(IReadOnlyList<string> arguments);

        /// <summary>
        /// Sets the standard input.
        /// </summary>
        ICli SetStandardInput(Stream standardInput);

        /// <summary>
        /// Sets the standard input to given string using given encoding.
        /// </summary>
        ICli SetStandardInput(string standardInput, Encoding encoding);

        /// <summary>
        /// Sets the standard input to given string.
        /// </summary>
        ICli SetStandardInput(string standardInput);

        /// <summary>
        /// Sets an environment variable to the given value.
        /// Can be called more than once to set multiple environment variables.
        /// </summary>
        ICli SetEnvironmentVariable(string key, string value);

        /// <summary>
        /// Sets the text encoding used for standard output stream.
        /// </summary>
        ICli SetStandardOutputEncoding(Encoding encoding);

        /// <summary>
        /// Sets the text encoding used for standard error stream.
        /// </summary>
        ICli SetStandardErrorEncoding(Encoding encoding);

        /// <summary>
        /// Sets the delegate that will be called whenever a new line is appended to standard output stream.
        /// </summary>
        ICli SetStandardOutputCallback(Action<string> callback);

        /// <summary>
        /// Sets the delegate that will be called whenever the standard output stream is closed.
        /// </summary>
        ICli SetStandardOutputClosedCallback(Action callback);

        /// <summary>
        /// Sets the delegate that will be called whenever a new line is appended to standard error stream.
        /// </summary>
        ICli SetStandardErrorCallback(Action<string> callback);

        /// <summary>
        /// Sets the delegate that will be called whenever the standard error stream is closed.
        /// </summary>
        ICli SetStandardErrorClosedCallback(Action callback);

        /// <summary>
        /// Sets the cancellation token.
        /// </summary>
        ICli SetCancellationToken(CancellationToken token);

        /// <summary>
        /// Enables or disables validation that will throw <see cref="ExitCodeValidationException"/> if the resulting exit code is not zero.
        /// </summary>
        ICli EnableExitCodeValidation(bool isEnabled = true);

        /// <summary>
        /// Enables or disables validation that will throw <see cref="StandardErrorValidationException"/> if the resulting standard error is not empty.
        /// </summary>
        ICli EnableStandardErrorValidation(bool isEnabled = true);

        /// <summary>
        /// Executes the process and synchronously waits for it to exit.
        /// </summary>
        ExecutionResult Execute();

        /// <summary>
        /// Executes the process and asynchronously waits for it to exit.
        /// </summary>
        Task<ExecutionResult> ExecuteAsync();

        /// <summary>
        /// Executes the process and doesn't wait for it to exit.
        /// </summary>
        void ExecuteAndForget();
    }
}