using System;
using CliWrap.Internal;
using JetBrains.Annotations;

namespace CliWrap.Models
{
    /// <summary>
    /// Output produced by executing a process.
    /// </summary>
    public class ExecutionResult
    {
        /// <summary>
        /// Process exit code.
        /// </summary>
        public int ExitCode { get; }

        /// <summary>
        /// Standard output data.
        /// </summary>
        [NotNull]
        public string StandardOutput { get; }

        /// <summary>
        /// Standard error data.
        /// </summary>
        [NotNull]
        public string StandardError { get; }

        /// <summary>
        /// Time at which this execution started.
        /// </summary>
        public DateTimeOffset StartTime { get; }

        /// <summary>
        /// Time at which this execution finished.
        /// </summary>
        public DateTimeOffset ExitTime { get; }

        /// <summary>
        /// Duration of this execution.
        /// </summary>
        public TimeSpan RunTime => ExitTime - StartTime;

        /// <summary>
        /// Initializes an instance of <see cref="ExecutionResult"/> with given output data.
        /// </summary>
        public ExecutionResult(int exitCode, string standardOutput, string standardError,
            DateTimeOffset startTime, DateTimeOffset exitTime)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput.GuardNotNull(nameof(standardOutput));
            StandardError = standardError.GuardNotNull(nameof(standardError));
            StartTime = startTime;
            ExitTime = exitTime;
        }
    }
}