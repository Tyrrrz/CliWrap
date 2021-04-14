using System;

namespace CliWrap
{
    /// <summary>
    /// Strategy used for validating the result of command execution.
    /// </summary>
    [Flags]
    public enum CommandResultValidation
    {
        /// <summary>
        /// No validation.
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