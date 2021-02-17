using System;
using System.Collections.Generic;
using System.Text;

namespace CliWrap
{
    /// <summary>
    /// Configuration of a command.
    /// </summary>
    public interface ICommandConfiguration
    {
        /// <summary>
        /// File path of the executable, batch file, or script, that this command runs on.
        /// </summary>
        string TargetFilePath { get; }

        /// <summary>
        /// Arguments passed on the command line.
        /// </summary>
        string Arguments { get; }

        /// <summary>
        /// Working directory path.
        /// </summary>
        string WorkingDirPath { get; }

        /// <summary>
        /// User credentials set for the underlying process.
        /// </summary>
        Credentials Credentials { get; }

        /// <summary>
        /// Environment variables set for the underlying process.
        /// </summary>
        IReadOnlyDictionary<string, string> EnvironmentVariables { get; }
    }
}
