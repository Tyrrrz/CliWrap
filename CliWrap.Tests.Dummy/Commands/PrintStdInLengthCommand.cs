using System.Buffers;
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
        using var buffer = MemoryPool<byte>.Shared.Rent(81920);

        var totalBytesRead = 0L;
        while (true)
        {
            var bytesRead = await console.Input.BaseStream.ReadAsync(buffer.Memory);
            if (bytesRead <= 0)
                break;

            totalBytesRead += bytesRead;
        }

        foreach (var writer in console.GetWriters(Target))
            await writer.WriteLineAsync(totalBytesRead.ToString(CultureInfo.InvariantCulture));
    }
}
