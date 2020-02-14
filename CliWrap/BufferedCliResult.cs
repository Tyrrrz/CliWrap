using System;

namespace CliWrap
{
    public class BufferedCliResult
    {
        public int ExitCode { get; }

        public DateTimeOffset StartTime { get; }

        public DateTimeOffset ExitTime { get; }

        public TimeSpan RunTime => ExitTime - StartTime;

        public string StandardOutput { get; }

        public string StandardError { get; }

        public BufferedCliResult(int exitCode, DateTimeOffset startTime, DateTimeOffset exitTime,
            string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StartTime = startTime;
            ExitTime = exitTime;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }
    }
}