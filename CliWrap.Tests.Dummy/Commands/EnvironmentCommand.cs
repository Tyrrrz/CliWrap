using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace CliWrap.Tests.Dummy.Commands;

[Command("env")]
public class EnvironmentCommand : ICommand
{
    [CommandParameter(0)]
    public IReadOnlyList<string> Names { get; init; } = [];

    public async ValueTask ExecuteAsync(IConsole console)
    {
        foreach (var name in Names)
            await console.Output.WriteLineAsync(Environment.GetEnvironmentVariable(name));
    }
}
