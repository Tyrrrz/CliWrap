using System;
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
        var buffer = new byte[81920];
        var bytesCopied = 0L;

        while (bytesCopied < Length)
        {
            var bytesToRead = (int)Math.Min(buffer.Length, Length - bytesCopied);

            var bytesRead = await console.Input.BaseStream.ReadAsync(buffer.AsMemory(0, bytesToRead));
            if (bytesRead <= 0)
                break;

            foreach (var writer in console.GetWriters(Target))
            {
                await writer.BaseStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            }

            bytesCopied += bytesRead;
        }
    }
}