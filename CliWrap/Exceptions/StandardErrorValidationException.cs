using System;
using System.Text;
using CliWrap.Models;

namespace CliWrap.Exceptions
{
    /// <summary>
    /// Thrown if underlying process reported an error.
    /// </summary>
    public partial class StandardErrorValidationException : ExecutionResultValidationException
    {
        /// <summary>
        /// Standard error data produced by underlying process.
        /// </summary>
        public string StandardError => ExecutionResult.StandardError;

        /// <summary>
        /// Initializes an instance of <see cref="StandardErrorValidationException"/>.
        /// </summary>
        public StandardErrorValidationException(ExecutionResult executionResult)
            : base(executionResult, CreateExceptionMessage(executionResult))
        {
        }
    }

    public partial class StandardErrorValidationException
    {
        private static string CreateExceptionMessage(ExecutionResult executionResult)
        {
            var buffer = new StringBuilder();
            buffer.AppendLine("Underlying process reported an error.")
                  .AppendLine($"Exit code: {executionResult.ExitCode}.")
                  .AppendLine("Standard error:")
                  .AppendLine(executionResult.StandardError);

            return buffer.ToString();
        }
    }
}