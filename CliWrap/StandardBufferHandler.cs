using System;

namespace CliWrap
{
    /// <summary>
    /// The default implementation of <see cref="IStandardBufferHandler"/>
    /// </summary>
    public class StandardBufferHandler : IStandardBufferHandler
    {
        private readonly Action<string> _standardOutputHandler;
        private readonly Action<string> _standardErrorHandler;

        /// <summary>
        /// Sets up a standard buffer handler using two callbacks. Both callbacks are optional.
        /// </summary>
        /// <param name="standardOutputHandler">The callback to use when a line is written to the standard output</param>
        /// <param name="standardErrorHandler">The callback to use when a line is written to the standard error</param>
        public StandardBufferHandler(Action<string> standardOutputHandler = null, Action<string> standardErrorHandler = null)
        {
            _standardOutputHandler = standardOutputHandler;
            _standardErrorHandler = standardErrorHandler;
        }

        /// <inheritdoc />
        public void HandleStandardOutput(string line)
        {
            _standardOutputHandler?.Invoke(line);
        }

        /// <inheritdoc />
        public void HandleStandardError(string line)
        {
            _standardErrorHandler?.Invoke(line);
        }
    }
}
