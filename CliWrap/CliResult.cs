using System;

namespace CliWrap
{
    public class CliResult
    {
        public int ExitCode { get; }

        public DateTimeOffset StartTime { get; }

        public DateTimeOffset ExitTime { get; }

        public TimeSpan RunTime => ExitTime - StartTime;

        public CliResult(int exitCode, DateTimeOffset startTime, DateTimeOffset exitTime)
        {
            ExitCode = exitCode;
            StartTime = startTime;
            ExitTime = exitTime;
        }
    }
}