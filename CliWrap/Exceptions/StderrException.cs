using System;

namespace CliWrap.Exceptions
{
    /// <summary>
    /// Thrown by the wrapped process when an error occurs
    /// </summary>
    public class StdErrException : Exception
    {
        /// <summary>
        /// Error stream output
        /// </summary>
        public string StdErr { get; }

        /// <inheritodoc />
        public StdErrException(string stdErr)
            : base("Wrapped process reported an error")
        {
            StdErr = stdErr;
        }
    }
}