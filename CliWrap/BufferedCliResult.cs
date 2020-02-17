using System;

namespace CliWrap
{
    public class BufferedCliResult : CliResult
    {
        public string StandardOutput { get; }

        public string StandardError { get; }

        public BufferedCliResult(
            int exitCode,
            DateTimeOffset startTime, DateTimeOffset exitTime,
            string standardOutput, string standardError)
            : base(exitCode, startTime, exitTime)
        {
            StandardOutput = standardOutput;
            StandardError = standardError;
        }
    }
}