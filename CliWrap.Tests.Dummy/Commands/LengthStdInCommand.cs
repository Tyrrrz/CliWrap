using System.Buffers;
using System.Globalization;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace CliWrap.Tests.Dummy.Commands;

[Command("length stdin")]
public class LengthStdInCommand : ICommand
{
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

        await console.Output.WriteLineAsync(totalBytesRead.ToString(CultureInfo.InvariantCulture));
    }
}
