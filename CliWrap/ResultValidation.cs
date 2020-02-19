using System;

namespace CliWrap
{
    [Flags]
    public enum ResultValidation
    {
        None = 0b0,
        ZeroExitCode = 0b1
    }

    public static class ResultValidationExtensions
    {
        public static bool IsZeroExitCodeValidationEnabled(this ResultValidation validation) =>
            (validation & ResultValidation.ZeroExitCode) != 0;
    }
}