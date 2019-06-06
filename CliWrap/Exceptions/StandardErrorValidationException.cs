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
            buffer.Append("Underlying process reported an error. ")
                .AppendLine("You can suppress this validation by calling EnableStandardErrorValidation(false).")
                .AppendLine()
                .Append("Exit code: ").Append(executionResult.ExitCode).AppendLine()
                .Append("Standard error: ").AppendLine(executionResult.StandardError);

            return buffer.ToString();
        }
    }
}