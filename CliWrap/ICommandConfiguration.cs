using System.Collections.Generic;

namespace CliWrap;

/// <summary>
/// Configuration of a command.
/// </summary>
public interface ICommandConfiguration
{
    /// <summary>
    /// File path of the executable, batch file, or script, that this command runs.
    /// </summary>
    string TargetFilePath { get; }

    /// <summary>
    /// Arguments passed on the command line.
    /// </summary>
    string Arguments { get; }

    /// <summary>
    /// Working directory path set for the underlying process.
    /// </summary>
    string WorkingDirPath { get; }

    /// <summary>
    /// User credentials set for the underlying process.
    /// </summary>
    Credentials Credentials { get; }

    /// <summary>
    /// Environment variables set for the underlying process.
    /// </summary>
    IReadOnlyDictionary<string, string?> EnvironmentVariables { get; }

    /// <summary>
    /// Configured result validation strategy.
    /// </summary>
    CommandResultValidation Validation { get; }

    /// <summary>
    /// Configured standard input pipe source.
    /// </summary>
    PipeSource StandardInputPipe { get; }

    /// <summary>
    /// Configured standard output pipe target.
    /// </summary>
    PipeTarget StandardOutputPipe { get; }

    /// <summary>
    /// Configured standard error pipe target.
    /// </summary>
    PipeTarget StandardErrorPipe { get; }
}