#if NET45 || NETSTANDARD2_0
using System.Collections.Generic;
#endif

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

#if NET45 || NETSTANDARD2_0
        /// <summary>
        /// Environment variables
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; } = new Dictionary<string, string>();
#endif

        /// <summary />
        public ExecutionInput(string arguments = null, string standardInput = null)
        {
            Arguments = arguments;
            StandardInput = standardInput;
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