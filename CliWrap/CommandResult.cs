using System;

namespace CliWrap
{
    /// <summary>
    /// Result of command execution.
    /// </summary>
    public class CommandResult
    {
        /// <summary>
        /// Exit code set by the underlying process.
        /// </summary>
        public int ExitCode { get; }

        /// <summary>
        /// Point in time at which the command has started executing.
        /// </summary>
        public DateTimeOffset StartTime { get; }

        /// <summary>
        /// Point in time at which the command has finished executing.
        /// </summary>
        public DateTimeOffset ExitTime { get; }

        /// <summary>
        /// Total duration of the execution.
        /// </summary>
        public TimeSpan RunTime => ExitTime - StartTime;

        /// <summary>
        /// Initializes an instance of <see cref="CommandResult"/>.
        /// </summary>
        public CommandResult(int exitCode, DateTimeOffset startTime, DateTimeOffset exitTime)
        {
            ExitCode = exitCode;
            StartTime = startTime;
            ExitTime = exitTime;
        }
    }
}