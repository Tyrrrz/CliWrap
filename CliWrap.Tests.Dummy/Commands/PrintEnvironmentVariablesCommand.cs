using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace CliWrap.Tests.Dummy.Commands;

[Command("print env")]
public class PrintEnvironmentVariablesCommand : ICommand
{
    public async ValueTask ExecuteAsync(IConsole console)
    {
        foreach (var (name, value) in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>())
        {
            await console.Output.WriteLineAsync($"[{name}] = {value}");
        }
    }
}