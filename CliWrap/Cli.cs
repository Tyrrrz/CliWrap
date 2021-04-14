namespace CliWrap
{
    // The only reason this entry point exists is because it looks cool to have
    // an expression that matches the library name -- Cli.Wrap().

    /// <summary>
    /// Main entry point for creating new commands.
    /// </summary>
    public static class Cli
    {
        /// <summary>
        /// Creates a new command that targets the specified command line executable, batch file, or script.
        /// </summary>
        public static Command Wrap(string targetFilePath) => new(targetFilePath);
    }
}