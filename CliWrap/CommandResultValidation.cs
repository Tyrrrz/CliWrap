using System;

namespace CliWrap
{
    /// <summary>
    /// Specifies enabled validations that run on the result of command execution.
    /// </summary>
    [Flags]
    public enum CommandResultValidation
    {
        /// <summary>
        /// No validations.
        /// </summary>
        None = 0b0,

        /// <summary>
        /// Ensure that the command returned a zero exit code.
        /// </summary>
        ZeroExitCode = 0b1
    }

    internal static class CommandResultValidationExtensions
    {
        public static bool IsZeroExitCodeValidationEnabled(this CommandResultValidation validation) =>
            (validation & CommandResultValidation.ZeroExitCode) != 0;
    }
}