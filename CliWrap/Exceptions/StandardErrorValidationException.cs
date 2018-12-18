using System;
using CliWrap.Models;

namespace CliWrap.Exceptions
{
    /// <summary>
    /// Thrown if underlying process reported an error.
    /// </summary>
    public class StandardErrorValidationException : ExecutionResultValidationException
    {
        /// <summary>
        /// Standard error data produced by underlying process.
        /// </summary>
        public string StandardError => ExecutionResult.StandardError;

        /// <summary>
        /// Initializes an instance of <see cref="StandardErrorValidationException"/>.
        /// </summary>
        public StandardErrorValidationException(ExecutionResult executionResult)
            : base(executionResult,
                $"Underlying process reported an error:{Environment.NewLine}{executionResult.StandardError}")
        {
        }
    }
}