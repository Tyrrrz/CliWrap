using System;
using System.Threading;
using CliWrap.Utils;

namespace CliWrap;

// The only reason this entry point exists is because it looks cool to have
// an expression that matches the library name -- Cli.Wrap().

/// <summary>
/// Main entry point for creating new commands.
/// </summary>
public static class Cli
{
    /// <summary>
    /// Cancellation token that is triggered when the current process is shutting down.
    /// </summary>
    /// <remarks>
    /// Pass this token to <c>ExecuteAsync(...)</c> to make sure the underlying process does not outlive
    /// the current one when it exits normally (e.g. by returning from <c>Main</c> or calling
    /// <see cref="Environment.Exit(int)" />) or receives <c>SIGTERM</c>.
    /// The token is not triggered when the current process is killed forcefully or crashes.
    /// </remarks>
    public static CancellationToken ShutdownToken => ShutdownSignal.Token;

    /// <summary>
    /// Creates a new command that targets the specified command-line executable, batch file, or script.
    /// </summary>
    public static Command Wrap(string targetFilePath) => new(targetFilePath);
}
