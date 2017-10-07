using System.Collections.Generic;

namespace CliWrap.Models
{
    /// <summary>
    /// Input used when executing a command line interface
    /// </summary>
    public partial class ExecutionInput
    {
        /// <summary>
        /// Command line arguments
        /// </summary>
        public string Arguments { get; }

        /// <summary>
        /// Standard input
        /// </summary>
        public string StandardInput { get; }

        /// <summary>
        /// Environment variables for the process
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; }

        /// <inheritdoc />
        public ExecutionInput(string arguments = null, string standardInput = null, IDictionary<string, string> envVars = null)
        {
            Arguments = arguments;
            StandardInput = standardInput;
            EnvironmentVariables = envVars ?? new Dictionary<string, string>();
        }
    }

    public partial class ExecutionInput
    {
        /// <summary>
        /// Empty input
        /// </summary>
        public static ExecutionInput Empty { get; } = new ExecutionInput();
    }
}