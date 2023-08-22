using System;
using System.Buffers;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using CliWrap.Tests.Dummy.Commands.Shared;

namespace CliWrap.Tests.Dummy.Commands;

[Command("echo stdin")]
public class EchoStdInCommand : ICommand
{
    [CommandOption("target")]
    public OutputTarget Target { get; init; } = OutputTarget.StdOut;

    [CommandOption("length")]
    public long Length { get; init; } = long.MaxValue;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        using var buffer = MemoryPool<byte>.Shared.Rent(81920);

        var totalBytesRead = 0L;
        while (totalBytesRead < Length)
        {
            var bytesWanted = (int)Math.Min(buffer.Memory.Length, Length - totalBytesRead);

            var bytesRead = await console.Input.BaseStream.ReadAsync(buffer.Memory[..bytesWanted]);
            if (bytesRead <= 0)
                break;

            foreach (var writer in console.GetWriters(Target))
                await writer.BaseStream.WriteAsync(buffer.Memory[..bytesRead]);

            totalBytesRead += bytesRead;
        }
    }
}
