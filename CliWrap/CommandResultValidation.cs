using System;

namespace CliWrap
{
    [Flags]
    public enum CommandResultValidation
    {
        None = 0b0,
        ZeroExitCode = 0b1
    }

    public static class CommandResultValidationExtensions
    {
        public static bool IsZeroExitCodeValidationEnabled(this CommandResultValidation validation) =>
            (validation & CommandResultValidation.ZeroExitCode) != 0;
    }
}