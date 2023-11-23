using System.IO;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace CliWrap.Tests.Dummy.Commands;

[Command("cwd")]
public class WorkingDirectoryCommand : ICommand
{
    public async ValueTask ExecuteAsync(IConsole console) =>
        await console.Output.WriteLineAsync(Directory.GetCurrentDirectory());
}
