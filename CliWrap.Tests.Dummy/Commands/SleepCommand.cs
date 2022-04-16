using System;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace CliWrap.Tests.Dummy.Commands;

[Command("sleep")]
public class SleepCommand : ICommand
{
    [CommandOption("duration")]
    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(1);

    public async ValueTask ExecuteAsync(IConsole console)
    {
        await console.Output.WriteLineAsync($"Sleeping for {Duration}...");
        await Task.Delay(Duration);
        await console.Output.WriteLineAsync("Done.");
    }
}