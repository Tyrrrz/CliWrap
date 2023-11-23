using CliWrap.Buffered;

namespace CliWrap.Magic;

public class MagicalCommandResult : BufferedCommandResult
{
    /// <summary>
    /// Converts the result to an integer value that corresponds to the <see cref="CommandResult.ExitCode" /> property.
    /// </summary>
    public static implicit operator int(MagicalCommandResult result) => result.ExitCode;

    /// <summary>
    /// Converts the result to a boolean value that corresponds to the <see cref="CommandResult.IsSuccess" /> property.
    /// </summary>
    public static implicit operator bool(MagicalCommandResult result) => result.IsSuccess;
}
