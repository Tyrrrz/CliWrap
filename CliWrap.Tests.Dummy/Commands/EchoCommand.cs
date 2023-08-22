using System.Collections.Generic;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using CliWrap.Tests.Dummy.Commands.Shared;

namespace CliWrap.Tests.Dummy.Commands;

[Command("echo")]
public class EchoCommand : ICommand
{
    [CommandParameter(0)]
    public required IReadOnlyList<string> Items { get; init; }

    [CommandOption("target")]
    public OutputTarget Target { get; init; } = OutputTarget.StdOut;

    [CommandOption("separator")]
    public string Separator { get; init; } = " ";

    public async ValueTask ExecuteAsync(IConsole console)
    {
        foreach (var writer in console.GetWriters(Target))
            await writer.WriteLineAsync(string.Join(Separator, Items));
    }
}
