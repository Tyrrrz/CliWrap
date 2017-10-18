using System;

namespace CliWrap.Services
{
    /// <summary>
    /// The default implementation of <see cref="IBufferHandler"/> which uses delegates
    /// </summary>
    public class BufferHandler : IBufferHandler
    {
        private readonly Action<string> _standardOutputHandler;
        private readonly Action<string> _standardErrorHandler;

        /// <summary />
        public BufferHandler(Action<string> standardOutputHandler = null, Action<string> standardErrorHandler = null)
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