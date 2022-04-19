using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace CliWrap.Tests.Dummy.Commands;

[Command("print-args")]
public class PrintArgsCommand : ICommand
{
    [CommandParameter(0)]
    public IReadOnlyList<string> Parameters { get; init; } = Array.Empty<string>();

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            await console.Output.WriteLineAsync($"[{i}] = {args[i]}");
        }
    }
}