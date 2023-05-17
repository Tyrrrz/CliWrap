using System.Globalization;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using CliWrap.Tests.Dummy.Commands.Shared;

namespace CliWrap.Tests.Dummy.Commands;

[Command("print length stdin")]
public class PrintStdInLengthCommand : ICommand
{
    [CommandOption("target")]
    public OutputTarget Target { get; init; } = OutputTarget.StdOut;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var length = 0L;
        var buffer = new byte[81920];

        while (true)
        {
            var bytesRead = await console.Input.BaseStream.ReadAsync(buffer);
            if (bytesRead <= 0)
                break;

            length += bytesRead;
        }

        foreach (var writer in console.GetWriters(Target))
            await writer.WriteLineAsync(length.ToString(CultureInfo.InvariantCulture));
    }
}