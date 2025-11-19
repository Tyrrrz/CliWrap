using System.Collections.Generic;

namespace CliWrap;

/// <summary>
/// Instructions for running a process.
/// </summary>
public interface ICommandConfiguration
{
    /// <summary>
    /// File path of the executable, batch file, or script, that this command runs.
    /// </summary>
    string TargetFilePath { get; }

    /// <summary>
    /// Command-line arguments passed to the underlying process.
    /// </summary>
    string Arguments { get; }

    /// <summary>
    /// Working directory path set for the underlying process.
    /// </summary>
    string WorkingDirPath { get; }

    /// <summary>
    /// Resource policy set for the underlying process.
    /// </summary>
    ResourcePolicy ResourcePolicy { get; }

    /// <summary>
    /// User credentials set for the underlying process.
    /// </summary>
    Credentials Credentials { get; }

    /// <summary>
    /// Environment variables set for the underlying process.
    /// </summary>
    IReadOnlyDictionary<string, string?> EnvironmentVariables { get; }

    /// <summary>
    /// Strategy for validating the result of the execution.
    /// </summary>
    CommandResultValidation Validation { get; }

    /// <summary>
    /// Pipe source for the standard input stream of the underlying process.
    /// </summary>
    PipeSource StandardInputPipe { get; }

    /// <summary>
    /// Pipe target for the standard output stream of the underlying process.
    /// </summary>
    PipeTarget StandardOutputPipe { get; }

    /// <summary>
    /// Pipe target for the standard error stream of the underlying process.
    /// </summary>
    PipeTarget StandardErrorPipe { get; }
}
