using System;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace CliWrap.Tests.Dummy.Commands;

[Command("sleep")]
public class SleepCommand : ICommand
{
    [CommandParameter(0)]
    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(1);

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var cancellationToken = console.RegisterCancellationHandler();

        try
        {
            await console.Output.WriteLineAsync($"Sleeping for {Duration}...");
            await Task.Delay(Duration, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await console.Output.WriteLineAsync("Canceled.");
            return;
        }

        await console.Output.WriteLineAsync("Done.");
    }
}
