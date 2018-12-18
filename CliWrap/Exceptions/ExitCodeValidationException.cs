using CliWrap.Models;

namespace CliWrap.Exceptions
{
    /// <summary>
    /// Thrown if underlying process reported a non-zero exit code.
    /// </summary>
    public class ExitCodeValidationException : ExecutionResultValidationException
    {
        /// <summary>
        /// Exit code reported by the underlying process.
        /// </summary>
        public int ExitCode => ExecutionResult.ExitCode;

        /// <summary>
        /// Initializes an instance of <see cref="ExitCodeValidationException"/>.
        /// </summary>
        public ExitCodeValidationException(ExecutionResult executionResult)
            : base(executionResult, $"Underlying process reported a non-zero exit code: {executionResult.ExitCode}")
        {
        }
    }
}