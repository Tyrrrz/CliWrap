using System;

namespace CliWrap.Buffered
{
    /// <summary>
    /// Result of command execution, with buffered data from standard output and standard error streams.
    /// </summary>
    public class BufferedCommandResult : CommandResult
    {
        /// <summary>
        /// Standard output data produced by the underlying process.
        /// </summary>
        public string StandardOutput { get; }

        /// <summary>
        /// Standard error data produced by the underlying process.
        /// </summary>
        public string StandardError { get; }

        /// <summary>
        /// Initializes an instance of <see cref="BufferedCommandResult"/>.
        /// </summary>
        public BufferedCommandResult(
            int exitCode,
            DateTimeOffset startTime,
            DateTimeOffset exitTime,
            string standardOutput,
            string standardError)
            : base(exitCode, startTime, exitTime)
        {
            StandardOutput = standardOutput;
            StandardError = standardError;
        }
    }
}