using System;
using System.Threading;

namespace CliWrap;

// The only reason this entry point exists is because it looks cool to have
// an expression that matches the library name -- Cli.Wrap().

/// <summary>
/// Main entry point for creating new commands.
/// </summary>
public static class Cli
{
    /// <summary>
    /// Cancellation token that is triggered when the current process exits in a managed way.
    /// </summary>
    /// <remarks>
    /// The token is triggered by <see cref="AppDomain.ProcessExit" />.
    /// Pass it to <c>ExecuteAsync(...)</c> to make sure the underlying process does not outlive
    /// the current one when it exits normally (e.g. by returning from <c>Main</c> or calling
    /// <see cref="Environment.Exit(int)" />).
    /// The token is not triggered when the current process is terminated by a signal it does not handle,
    /// killed forcefully, or crashed by an unhandled exception.
    /// </remarks>
    public static CancellationToken Shutdown => ShutdownSignal.Token;

    /// <summary>
    /// Creates a new command that targets the specified command-line executable, batch file, or script.
    /// </summary>
    public static Command Wrap(string targetFilePath) => new(targetFilePath);

    // Separate class so that the event subscription is only made when the token is actually used
    private static class ShutdownSignal
    {
        private static readonly CancellationTokenSource Cts = new();

        public static CancellationToken Token => Cts.Token;

        static ShutdownSignal() =>
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                try
                {
                    // Callbacks run synchronously, so the processes are killed before the handler returns
                    Cts.Cancel();
                }
                catch
                {
                    // The process is exiting anyway, so there's nothing useful to do with the exception
                }
            };
    }
}
