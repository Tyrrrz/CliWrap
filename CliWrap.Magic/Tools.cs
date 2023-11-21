using System;
using System.Collections.Generic;
using CliWrap.Magic.Contexts;
using Contextual;

namespace CliWrap.Magic;

/// <summary>
/// Utility methods for working with the shell environment.
/// </summary>
public static class Tools
{
    /// <summary>
    /// Creates a new command that targets the specified command-line executable, batch file, or script.
    /// </summary>
    public static Command Run(string targetFilePath) =>
        Cli.Wrap(targetFilePath)
            .WithWorkingDirectory(Context.Use<WorkingDirectoryContext>().Path)
            .WithEnvironmentVariables(Context.Use<EnvironmentVariablesContext>().Variables)
            .WithStandardInputPipe(Context.Use<StandardInputPipeContext>().Pipe)
            .WithStandardOutputPipe(Context.Use<StandardOutputPipeContext>().Pipe)
            .WithStandardErrorPipe(Context.Use<StandardErrorPipeContext>().Pipe);

    /// <summary>
    /// Creates a new command that targets the specified command-line executable, batch file, or script,
    /// with the provided command-line arguments.
    /// </summary>
    public static Command Run(
        string targetFilePath,
        IEnumerable<string> arguments,
        bool escape = true
    ) => Run(targetFilePath).WithArguments(arguments, escape);

    /// <summary>
    /// Changes the current working directory to the specified path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method only affects commands created by <see cref="Run(string)" />.
    /// It does not change the working directory of the current process.
    /// </para>
    /// <para>
    /// In order to reset the working directory to its original value, dispose the returned object.
    /// </para>
    /// </remarks>
    public static IDisposable ChangeDirectory(string workingDirPath) =>
        Context.Provide(new WorkingDirectoryContext(workingDirPath));

    /// <summary>
    /// Gets the value of the specified environment variable.
    /// </summary>
    /// <remarks>
    /// This method reads environment variables both from the context created by <see cref="Environment(string, string?)" />,
    /// as well as from the current process.
    /// </remarks>
    public static string? Environment(string name) =>
        Context.Use<EnvironmentVariablesContext>().Variables.TryGetValue(name, out var value)
            ? value
            : System.Environment.GetEnvironmentVariable(name);

    /// <summary>
    /// Sets the value of the specified environment variable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method only affects commands created by <see cref="Run(string)" />.
    /// It does not change the environment variables of the current process.
    /// </para>
    /// <para>
    /// In order to reset the environment variables to their original state, dispose the returned object.
    /// </para>
    /// </remarks>
    public static IDisposable Environment(string name, string? value)
    {
        var variables = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var (lastKey, lastValue) in Context.Use<EnvironmentVariablesContext>().Variables)
            variables[lastKey] = lastValue;

        variables[name] = value;

        return Context.Provide(new EnvironmentVariablesContext(variables));
    }
}
