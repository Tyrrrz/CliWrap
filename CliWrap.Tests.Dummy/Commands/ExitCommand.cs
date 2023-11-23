using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;

namespace CliWrap.Tests.Dummy.Commands;

[Command("exit")]
public class ExitCommand : ICommand
{
    [CommandParameter(0)]
    public int ExitCode { get; init; }

    public ValueTask ExecuteAsync(IConsole console)
    {
        if (ExitCode != 0)
            throw new CommandException($"Exit code set to {ExitCode}", ExitCode);

        return default;
    }
}
