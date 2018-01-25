using System.Collections.Generic;
using CliWrap.Internal;
using JetBrains.Annotations;

namespace CliWrap.Models
{
    /// <summary>
    /// Input used for executing a process.
    /// </summary>
    public class ExecutionInput
    {
        private IDictionary<string, string> _environmentVariables;

        /// <summary>
        /// Command line arguments.
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Standard input data.
        /// </summary>
        public string StandardInput { get; set; }

        /// <summary>
        /// Environment variables.
        /// </summary>
        [NotNull]
        public IDictionary<string, string> EnvironmentVariables
        {
            get => _environmentVariables;
            set => _environmentVariables = value.GuardNotNull(nameof(value));
        }

        /// <summary>
        /// Initializes <see cref="ExecutionInput"/> with given arguments and standard input data.
        /// </summary>
        public ExecutionInput(string arguments = null, string standardInput = null)
        {
            Arguments = arguments;
            StandardInput = standardInput;
            EnvironmentVariables = new Dictionary<string, string>();
        }
    }
}