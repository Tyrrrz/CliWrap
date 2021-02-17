using System;

namespace CliWrap.Exceptions
{
    /// <summary>
    /// Exception thrown when the command fails to execute correctly.
    /// </summary>
    public partial class CommandExecutionException : Exception
    {
        /// <summary>
        /// Command that triggered the exception.
        /// </summary>
        public ICommandConfiguration Command { get; }

        /// <summary>
        /// Exit code returned by the process.
        /// </summary>
        public int ExitCode { get; }

        /// <summary>
        /// Initializes an instance of <see cref="CommandExecutionException"/>.
        /// </summary>
        public CommandExecutionException(ICommandConfiguration command, int exitCode, string message) : base(message)
        {
            Command = command;
            ExitCode = exitCode;
        }
    }

    public partial class CommandExecutionException
    {
        internal static CommandExecutionException ExitCodeValidation(
            ICommandConfiguration command,
            int exitCode)
        {
            var message = @$"
Underlying process reported a non-zero exit code ({exitCode}).

Command:
  {command.TargetFilePath} {command.Arguments}

You can suppress this validation by calling `WithValidation(CommandResultValidation.None)` on the command.".Trim();

            return new CommandExecutionException(command, exitCode, message);
        }

        internal static CommandExecutionException ExitCodeValidation(
            ICommandConfiguration command,
            int exitCode,
            string standardError)
        {
            var message = @$"
Underlying process reported a non-zero exit code ({exitCode}).

Command:
  {command.TargetFilePath} {command.Arguments}

Standard error:
  {standardError}

You can suppress this validation by calling `WithValidation(CommandResultValidation.None)` on the command.".Trim();

            return new CommandExecutionException(command, exitCode, message);
        }
    }
}