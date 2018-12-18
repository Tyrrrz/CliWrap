using System;
using CliWrap.Internal;
using CliWrap.Models;

namespace CliWrap.Exceptions
{
    /// <summary>
    /// Base class for exceptions that are thrown when an instance of <see cref="ExecutionResult"/> fails a certain validation.
    /// </summary>
    public abstract class ExecutionResultValidationException : Exception
    {
        /// <summary>
        /// Execution result.
        /// </summary>
        public ExecutionResult ExecutionResult { get; }

        /// <summary>
        /// Initializes an instance of <see cref="ExecutionResultValidationException"/>.
        /// </summary>
        protected ExecutionResultValidationException(ExecutionResult executionResult, string message)
            : base(message)
        {
            ExecutionResult = executionResult.GuardNotNull(nameof(executionResult));
        }
    }
}