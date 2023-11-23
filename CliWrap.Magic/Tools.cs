using System;
using System.Collections.Generic;
using System.IO;
using CliWrap.Magic.Utils;

namespace CliWrap.Magic;

/// <summary>
/// Utility methods for working with the shell environment.
/// </summary>
public static partial class Tools
{
    /// <summary>
    /// Creates a new command that targets the specified command-line executable, batch file, or script.
    /// </summary>
    public static Command Command(string targetFilePath) =>
        Cli.Wrap(targetFilePath)
            .WithStandardInputPipe(DefaultStandardInputPipe)
            .WithStandardOutputPipe(DefaultStandardOutputPipe)
            .WithStandardErrorPipe(DefaultStandardErrorPipe);

    /// <summary>
    /// Creates a new command that targets the specified command-line executable, batch file, or script,
    /// with the provided command-line arguments.
    /// </summary>
    public static Command Command(string targetFilePath, string arguments) =>
        Command(targetFilePath).WithArguments(arguments);

    /// <summary>
    /// Creates a new command that targets the specified command-line executable, batch file, or script,
    /// with the provided command-line arguments.
    /// </summary>
    public static Command Command(
        string targetFilePath,
        IEnumerable<string> arguments,
        bool escape = true
    ) => Command(targetFilePath).WithArguments(arguments, escape);

    /// <summary>
    /// Gets the current working directory.
    /// </summary>
    public static string WorkingDirectory() => Directory.GetCurrentDirectory();

    /// <summary>
    /// Changes the current working directory to the specified path.
    /// </summary>
    /// <remarks>
    /// You can dispose the returned object to reset the path back to its previous value.
    /// </remarks>
    public static IDisposable WorkingDirectory(string path)
    {
        var lastPath = WorkingDirectory();
        Directory.SetCurrentDirectory(path);

        return Disposable.Create(() => Directory.SetCurrentDirectory(lastPath));
    }

    /// <summary>
    /// Gets the value of the specified environment variable.
    /// </summary>
    public static string? Environment(string name) =>
        System.Environment.GetEnvironmentVariable(name);

    /// <summary>
    /// Sets the value of the specified environment variable.
    /// </summary>
    /// <remarks>
    /// You can dispose the returned object to reset the environment variable back to its previous value.
    /// </remarks>
    public static IDisposable Environment(string name, string? value)
    {
        var lastValue = System.Environment.GetEnvironmentVariable(name);
        System.Environment.SetEnvironmentVariable(name, value);

        return Disposable.Create(() => System.Environment.SetEnvironmentVariable(name, lastValue));
    }
}

public partial class Tools
{
    /// <summary>
    /// Default standard input pipe used for commands created by <see cref="Command(string)" />.
    /// </summary>
    public static PipeSource DefaultStandardInputPipe { get; set; } =
        PipeSource.FromStream(Console.OpenStandardInput());

    /// <summary>
    /// Default standard output pipe used for commands created by <see cref="Command(string)" />.
    /// </summary>
    public static PipeTarget DefaultStandardOutputPipe { get; set; } =
        PipeTarget.ToStream(Console.OpenStandardOutput());

    /// <summary>
    /// Default standard error pipe used for commands created by <see cref="Command(string)" />.
    /// </summary>
    public static PipeTarget DefaultStandardErrorPipe { get; set; } =
        PipeTarget.ToStream(Console.OpenStandardError());
}
