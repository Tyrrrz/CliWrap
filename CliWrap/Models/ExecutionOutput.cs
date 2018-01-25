using System;
using CliWrap.Exceptions;
using CliWrap.Internal;
using JetBrains.Annotations;

namespace CliWrap.Models
{
    /// <summary>
    /// Output produced by executing a process.
    /// </summary>
    public class ExecutionOutput
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
        /// Whether the process reported any errors.
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(StandardError);

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
        /// Initializes <see cref="ExecutionOutput"/> with given output data.
        /// </summary>
        public ExecutionOutput(int exitCode, string standardOutput, string standardError,
            DateTimeOffset startTime, DateTimeOffset exitTime)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput.GuardNotNull(nameof(standardOutput));
            StandardError = standardError.GuardNotNull(nameof(standardError));
            StartTime = startTime;
            ExitTime = exitTime;
        }

        /// <summary>
        /// Throws <see cref="StandardErrorException"/> if the underlying process reported an error during this execution.
        /// </summary>
        public void ThrowIfError()
        {
            if (HasError)
                throw new StandardErrorException(StandardError);
        }
    }
}