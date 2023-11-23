using System.Runtime.InteropServices;

namespace CliWrap;

/// <summary>
/// Main entry point for creating new commands.
/// </summary>
public static class Cli
{
    /// <summary>
    /// Creates a new command that targets the specified command-line executable, batch file, or script.
    /// </summary>
    public static Command Wrap(string targetFilePath) => new(targetFilePath);

    /// <summary>
    /// Creates a new command that wraps the default system shell with the specified input.
    /// </summary>
    /// <remarks>
    /// On Windows, it uses <c>cmd.exe</c>.
    /// On Linux and macOS, it uses <c>/bin/sh</c>.
    /// </remarks>
    public static Command WrapShell(string input) =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Wrap("cmd.exe").WithArguments(new[] { "/c", input })
            : Wrap("/bin/sh").WithArguments(new[] { "-c", input });
}
