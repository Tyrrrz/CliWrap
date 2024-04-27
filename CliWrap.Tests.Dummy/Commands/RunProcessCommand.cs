using System.Diagnostics;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace CliWrap.Tests.Dummy.Commands;

[Command("run process")]
public class RunProcessCommand : ICommand
{
    [CommandOption("path")]
    public string FilePath { get; init; } = string.Empty;

    [CommandOption("arguments")]
    public string Arguments { get; init; } = string.Empty;

    public ValueTask ExecuteAsync(IConsole console)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = FilePath,
            Arguments = Arguments,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        return default;
    }
}
