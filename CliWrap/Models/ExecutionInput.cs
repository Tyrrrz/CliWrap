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
        public string Arguments { get; set; }

        /// <summary>
        /// Standard input
        /// </summary>
        public string StandardInput { get; set; }

#if NET45 || NETSTANDARD2_0
        /// <summary>
        /// Environment variables
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; }
#endif

        /// <summary />
        public ExecutionInput(string arguments = null)
        {
            Arguments = arguments;
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