using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CliWrap.Magic.Utils;

namespace CliWrap.Magic;

/// <summary>
/// Utility methods for working with the shell environment.
/// </summary>
public static class Prelude
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

    /// <summary>
    /// Creates a new command with the specified target file path.
    /// </summary>
    public static Command _(string targetFilePath) =>
        Cli.Wrap(targetFilePath)
            .WithStandardInputPipe(DefaultStandardInputPipe)
            .WithStandardOutputPipe(DefaultStandardOutputPipe)
            .WithStandardErrorPipe(DefaultStandardErrorPipe);

    /// <summary>
    /// Creates a new command with the specified target file path and command-line arguments.
    /// </summary>
    public static Command _(string targetFilePath, IEnumerable<string> arguments) =>
        _(targetFilePath).WithArguments(arguments);

    /// <summary>
    /// Creates a new command with the specified target file path and command-line arguments.
    /// </summary>
    public static Command _(string targetFilePath, params string[] arguments) =>
        _(targetFilePath, (IEnumerable<string>)arguments);

    /// <summary>
    /// Creates a new command with the specified target file path and command-line arguments.
    /// </summary>
    public static Command _(string targetFilePath, IEnumerable<Stringish> arguments) =>
        _(targetFilePath).WithArguments(a => a.Add(arguments.Select(x => x.ToString())));

    /// <summary>
    /// Creates a new command with the specified target file path and command-line arguments.
    /// </summary>
    public static Command _(string targetFilePath, params Stringish[] arguments) =>
        _(targetFilePath, (IEnumerable<Stringish>)arguments);

    private static Command Shell(Command command) =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Cli.Wrap("cmd.exe")
                .WithArguments(new[] { "/c", command.TargetFilePath, command.Arguments })
            : Cli.Wrap("/bin/sh")
                .WithArguments(new[] { "-c", command.TargetFilePath, command.Arguments });

    /// <summary>
    /// Creates a new command with the specified target file path, wrapped in the default system shell.
    /// </summary>
    /// <remarks>
    /// The default system shell is determined based on the current operating system:
    /// <c>cmd.exe</c> on Windows, <c>/bin/sh</c> on Linux and macOS.
    /// </remarks>
    public static Command Shell(string targetFilePath) => Shell(_(targetFilePath, "-c"));

    /// <summary>
    /// Creates a new command with the specified target file path and command-line arguments,
    /// wrapped in the default system shell.
    /// </summary>
    /// <remarks>
    /// The default system shell is determined based on the current operating system:
    /// <c>cmd.exe</c> on Windows, <c>/bin/sh</c> on Linux and macOS.
    /// </remarks>
    public static Command Shell(string targetFilePath, IEnumerable<string> arguments) =>
        Shell(_(targetFilePath).WithArguments(arguments));

    /// <summary>
    /// Creates a new command with the specified target file path and command-line arguments,
    /// wrapped in the default system shell.
    /// </summary>
    /// <remarks>
    /// The default system shell is determined based on the current operating system:
    /// <c>cmd.exe</c> on Windows, <c>/bin/sh</c> on Linux and macOS.
    /// </remarks>
    public static Command Shell(string targetFilePath, params string[] arguments) =>
        Shell(_(targetFilePath, (IEnumerable<string>)arguments));

    /// <summary>
    /// Creates a new command with the specified target file path and command-line arguments,
    /// wrapped in the default system shell.
    /// </summary>
    /// <remarks>
    /// The default system shell is determined based on the current operating system:
    /// <c>cmd.exe</c> on Windows, <c>/bin/sh</c> on Linux and macOS.
    /// </remarks>
    public static Command Shell(string targetFilePath, IEnumerable<Stringish> arguments) =>
        Shell(_(targetFilePath).WithArguments(a => a.Add(arguments.Select(x => x.Value))));

    /// <summary>
    /// Creates a new command with the specified target file path and command-line arguments,
    /// wrapped in the default system shell.
    /// </summary>
    /// <remarks>
    /// The default system shell is determined based on the current operating system:
    /// <c>cmd.exe</c> on Windows, <c>/bin/sh</c> on Linux and macOS.
    /// </remarks>
    public static Command Shell(string targetFilePath, params Stringish[] arguments) =>
        Shell(_(targetFilePath, (IEnumerable<Stringish>)arguments));

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

    /// <summary>
    /// Terminates the current process with the specified exit code.
    /// </summary>
    public static void Exit(int exitCode = 0) => System.Environment.Exit(exitCode);

    /// <summary>
    /// Prompt the user for input, with an optional message.
    /// </summary>
    public static string? ReadLine(string? message = null)
    {
        if (!string.IsNullOrWhiteSpace(message))
            Console.Write(message);

        return Console.ReadLine();
    }

    /// <summary>
    /// Writes the specified text to the standard output stream.
    /// </summary>
    public static void Write(string text) => Console.Write(text);

    /// <summary>
    /// Writes the specified text to the standard output stream, followed by a line terminator.
    /// </summary>
    public static void WriteLine(string text) => Write(text + System.Environment.NewLine);

    /// <summary>
    /// Writes the specified text to the standard error stream.
    /// </summary>
    public static void WriteError(string text) => Console.Error.Write(text);

    /// <summary>
    /// Writes the specified text to the standard error stream, followed by a line terminator.
    /// </summary>
    public static void WriteErrorLine(string text) => WriteError(text + System.Environment.NewLine);

    /// <summary>
    /// Checks if the specified value is truthy.
    /// </summary>
    public static bool IsTruthy(bool value) => value;

    /// <summary>
    /// Checks if the specified value is truthy.
    /// </summary>
    public static bool IsTruthy(string? value) => !string.IsNullOrEmpty(value);

    /// <summary>
    /// Checks if the specified value is truthy.
    /// </summary>
    public static bool IsTruthy(int? value) => value.HasValue && value.Value != default;

    /// <summary>
    /// Checks if the specified value is truthy.
    /// </summary>
    public static bool IsTruthy(long? value) => value.HasValue && value.Value != default;

    /// <summary>
    /// Checks if the specified value is truthy.
    /// </summary>
    public static bool IsTruthy<T>(IEnumerable<T> values) => values.Any();

    /// <summary>
    /// Checks if the specified value is truthy.
    /// </summary>
    public static bool IsTruthy(object? value) =>
        value switch
        {
            null => false,
            bool x => IsTruthy(x),
            string x => IsTruthy(x),
            int x => IsTruthy(x),
            long x => IsTruthy(x),
            IEnumerable<object> x => IsTruthy(x),
            _ => true
        };
}
