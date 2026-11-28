using System.Collections.Generic;
using System.Threading.Tasks;
using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;

namespace CliWrap.Tests.Dummy.Commands;

// Starts the specified process bound to Cli.ShutdownToken and prints its ID
[Command("spawn")]
public partial class SpawnCommand : ICommand
{
    [CommandParameter(0)]
    public required string FilePath { get; set; }

    [CommandParameter(1)]
    public IReadOnlyList<string> Arguments { get; set; } = [];

    [CommandOption("wait", Description = "Wait for the spawned process to exit.")]
    public bool Wait { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var task = Cli.Wrap(FilePath)
            .WithArguments(Arguments)
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(Cli.ShutdownToken);

        await console.Output.WriteLineAsync(task.ProcessId.ToString());

        if (Wait)
            await task;
    }
}
