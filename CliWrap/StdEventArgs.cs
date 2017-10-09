using System;

namespace CliWrap
{
    /// <summary>
    /// Event aguments for new output data received
    /// </summary>
    public class StdEventArgs
    {
        /// <summary>
        /// Costructs an StdEventArgs class with a text line of output
        /// </summary>
        /// <param name="data"></param>
        public StdEventArgs(string data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <summary>
        /// The line of text that got written to the output stream
        /// </summary>
        public string Data { get; }
    }
}
