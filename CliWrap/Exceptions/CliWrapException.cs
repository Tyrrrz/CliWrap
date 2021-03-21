using System;

namespace CliWrap.Exceptions
{
    /// <summary>
    /// Parent class for domain exceptions thrown by <see cref="CliWrap"/>.
    /// </summary>
    public abstract class CliWrapException : Exception
    {
        /// <summary>
        /// Initializes an instance of <see cref="CliWrapException"/>.
        /// </summary>
        /// <param name="message"></param>
        protected CliWrapException(string message)
            : base(message)
        {
        }
    }
}