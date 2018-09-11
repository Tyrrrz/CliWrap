using System;

namespace CliWrap.Exceptions
{
    /// <summary>
    /// Thrown if underlying process reported a non-zero exit code.
    /// </summary>
    public class ExitCodeValidationException : Exception
    {
        /// <summary>
        /// Exit code reported by the underlying process.
        /// </summary>
        public int ExitCode { get; }

        /// <summary>
        /// Initializes <see cref="ExitCodeValidationException"/> with given exit code.
        /// </summary>
        public ExitCodeValidationException(int exitCode)
            : base($"Underlying process reported a non-zero exit code: {exitCode}")
        {
            ExitCode = exitCode;
        }
    }
}