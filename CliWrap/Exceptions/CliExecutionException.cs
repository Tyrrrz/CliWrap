using System;

namespace CliWrap.Exceptions
{
    public partial class CliExecutionException : Exception
    {
        public CliExecutionException(string message)
            : base(message)
        {
        }
    }

    public partial class CliExecutionException
    {
        public static CliExecutionException ExitCodeValidation(string filePath, string arguments, int exitCode, string standardError)
        {
            var message = @$"
Underlying process reported a non-zero exit code.
{filePath} {arguments}

Exit code: {exitCode}
Standard error: {standardError}

You can suppress this validation by calling `EnableExitCodeValidation(false)` when configuring the Cli.".Trim();

            return new CliExecutionException(message);
        }

        public static CliExecutionException ExitCodeValidation(string filePath, string arguments, int exitCode)
        {
            var message = @$"
Underlying process reported a non-zero exit code.
{filePath} {arguments}

Exit code: {exitCode}

You can suppress this validation by calling `EnableExitCodeValidation(false)` when configuring the Cli.".Trim();

            return new CliExecutionException(message);
        }
    }
}