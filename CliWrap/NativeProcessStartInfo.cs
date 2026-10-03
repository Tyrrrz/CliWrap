using System.Collections.Generic;

namespace CliWrap;

/// <summary>
/// Parameters passed to <see cref="NativeProcess" /> factory methods.
/// </summary>
internal sealed class NativeProcessStartInfo(
    string fileName,
    string arguments,
    string workingDirectory,
    IReadOnlyDictionary<string, string?> environmentVariables,
    PseudoConsoleOptions? pseudoConsoleOptions = null
)
{
    public string FileName { get; } = fileName;
    public string Arguments { get; } = arguments;
    public string WorkingDirectory { get; } = workingDirectory;
    public IReadOnlyDictionary<string, string?> EnvironmentVariables { get; } =
        environmentVariables;
    public PseudoConsoleOptions? PseudoConsoleOptions { get; } = pseudoConsoleOptions;
}
