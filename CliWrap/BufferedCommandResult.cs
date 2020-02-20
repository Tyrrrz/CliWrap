using System;

namespace CliWrap
{
    public class BufferedCommandResult : CommandResult
    {
        public string StandardOutput { get; }

        public string StandardError { get; }

        public BufferedCommandResult(
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