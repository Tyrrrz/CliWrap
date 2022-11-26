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

        var cancellationToken = console.RegisterCancellationHandler();

        try
        {
            await Task.Delay(Duration, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await console.Output.WriteLineAsync("Operation canceled gracefully.");
            return;
        }

        await console.Output.WriteLineAsync("Done.");
    }
}