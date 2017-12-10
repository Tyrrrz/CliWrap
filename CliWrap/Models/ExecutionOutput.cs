using System;
using CliWrap.Exceptions;
using CliWrap.Internal;

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
        public string StandardOutput { get; }

        /// <summary>
        /// Standard error data.
        /// </summary>
        public string StandardError { get; }

        /// <summary>
        /// Whether the process reported any errors.
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(StandardError);

        /// <summary>
        /// Time at which this execution started.
        /// </summary>
        public DateTime StartTime { get; }

        /// <summary>
        /// Time at which this execution finished.
        /// </summary>
        public DateTime ExitTime { get; }

        /// <summary>
        /// Duration of this execution.
        /// </summary>
        public TimeSpan RunTime => ExitTime - StartTime;

        /// <summary />
        public ExecutionOutput(int exitCode, string standardOutput, string standardError,
            DateTime startTime, DateTime exitTime)
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