using System;
using CliWrap.Internal;

namespace CliWrap.Exceptions
{
    /// <summary>
    /// Thrown if underlying process reported an error.
    /// </summary>
    public class StandardErrorException : Exception
    {
        /// <summary>
        /// Standard error data produced by underlying process.
        /// </summary>
        public string StandardError { get; }

        /// <inheritdoc />
        public override string Message { get; }

        /// <summary>
        /// Initializes <see cref="StandardErrorException"/> with given standard error data.
        /// </summary>
        public StandardErrorException(string standardError)
        {
            StandardError = standardError.GuardNotNull(nameof(standardError));
            Message = "Underlying process reported an error. " +
                      $"Inspect [{nameof(StandardError)}] property for more information.";
        }
    }
}