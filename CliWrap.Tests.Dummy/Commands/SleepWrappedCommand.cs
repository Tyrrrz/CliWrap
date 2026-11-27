using System;
using System.Threading.Tasks;
using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;

namespace CliWrap.Tests.Dummy.Commands;

// Starts a child process that sleeps and exits right away, without waiting for it.
// Used to verify that processes bound to Cli.Shutdown don't outlive their parent.
[Command("sleep-wrapped")]
public partial class SleepWrappedCommand : ICommand
{
    [CommandParameter(0)]
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(1);

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var task = Cli.Wrap(Program.FilePath)
            .WithArguments(["sleep", Duration.ToString()])
            .ExecuteAsync(Cli.Shutdown);

        await console.Output.WriteLineAsync(task.ProcessId.ToString());
    }
}
