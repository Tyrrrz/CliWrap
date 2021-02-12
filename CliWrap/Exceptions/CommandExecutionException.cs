using System;

namespace CliWrap.Exceptions
{
    /// <summary>
    /// Exception thrown when the command fails to execute correctly.
    /// </summary>
    public partial class CommandExecutionException : Exception
    {
        public string FilePath { get; }
        public string Arguments { get; }
        public int ExitCode { get; }
        public string? StandardError { get; }

        /// <summary>
        /// Initializes an instance of <see cref="CommandExecutionException"/>.
        /// </summary>
        public CommandExecutionException(string message, string filePath, string arguments, int exitCode, string? standardError = null) : base(message)
        {
            FilePath = filePath;
            Arguments = arguments;
            ExitCode = exitCode;
            StandardError = standardError;
        }
    }

    public partial class CommandExecutionException
    {
        internal static CommandExecutionException ExitCodeValidation(
            string filePath,
            string arguments,
            int exitCode)
        {
            var message = @$"
Underlying process reported a non-zero exit code ({exitCode}).

Command:
  {filePath} {arguments}

You can suppress this validation by calling `WithValidation(CommandResultValidation.None)` on the command.".Trim();

            return new CommandExecutionException(message, filePath, arguments, exitCode);
        }

        internal static CommandExecutionException ExitCodeValidation(
            string filePath,
            string arguments,
            int exitCode,
            string standardError)
        {
            var message = @$"
Underlying process reported a non-zero exit code ({exitCode}).

Command:
  {filePath} {arguments}

Standard error:
  {standardError}

You can suppress this validation by calling `WithValidation(CommandResultValidation.None)` on the command.".Trim();

            return new CommandExecutionException(message, filePath, arguments, exitCode, standardError);
        }
    }
}