using System;
using CliWrap.Internal;

namespace CliWrap.Exceptions
{
    /// <summary>
    /// Thrown when a command line executable reports an error
    /// </summary>
    public class StandardErrorException : Exception
    {
        /// <summary>
        /// Standard error
        /// </summary>
        public string StandardError { get; }

        /// <summary />
        public StandardErrorException(string standardError)
            : base("Command line executable reported an error")
        {
            StandardError = standardError.GuardNotNull(nameof(standardError));
        }
    }
}