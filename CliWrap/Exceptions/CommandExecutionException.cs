using System;

namespace CliWrap.Exceptions
{
    /// <summary>
    /// Exception thrown when the command fails to execute correctly.
    /// </summary>
    public partial class CommandExecutionException : Exception
    {
        public ICommandConfiguration Command { get; }
        public int ExitCode { get; }
        public string? StandardError { get; }

        /// <summary>
        /// Initializes an instance of <see cref="CommandExecutionException"/>.
        /// </summary>
        public CommandExecutionException(ICommandConfiguration command, int exitCode, string? standardError = null)
            : base(BuildMessage(command, exitCode, standardError))
        {
            Command = command;
            ExitCode = exitCode;
            StandardError = standardError;
        }

        private static string BuildMessage(ICommandConfiguration command, int exitCode, string? standardError)
        {
            var message = @$"
Underlying process reported a non-zero exit code ({exitCode}).

Command:
  {command.TargetFilePath} {command.Arguments}
";

            if (standardError != null)
                message += @$"
Standard error:
  {standardError}
";

            message += Environment.NewLine + "You can suppress this validation by calling `WithValidation(CommandResultValidation.None)` on the command.";

            return message.Trim();
        }
    }

    public partial class CommandExecutionException
    {
        internal static CommandExecutionException ExitCodeValidation(
            ICommandConfiguration command,
            int exitCode)
        {
            return new CommandExecutionException(command, exitCode);
        }

        internal static CommandExecutionException ExitCodeValidation(
            ICommandConfiguration command,
            int exitCode,
            string standardError)
        {
            return new CommandExecutionException(command, exitCode, standardError);
        }
    }
}