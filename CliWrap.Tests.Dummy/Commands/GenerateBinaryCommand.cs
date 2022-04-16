using System;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using CliWrap.Tests.Dummy.Commands.Shared;

namespace CliWrap.Tests.Dummy.Commands;

[Command("generate binary")]
public class GenerateBinaryCommand : ICommand
{
    // Tests rely on the random seed being fixed
    private readonly Random _random = new(1234567);

    [CommandOption("target")]
    public OutputTarget Target { get; init; } = OutputTarget.StdOut;

    [CommandOption("length")]
    public long Length { get; init; } = 1_000_000;

    [CommandOption("buffer")]
    public int BufferSize { get; init; } = 1024;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var buffer = new byte[BufferSize];
        var bytesRemaining = Length;

        while (bytesRemaining > 0)
        {
            _random.NextBytes(buffer);

            var bytesToWrite = Math.Min((int)bytesRemaining, buffer.Length);

            foreach (var writer in console.GetWriters(Target))
            {
                await writer.BaseStream.WriteAsync(buffer.AsMemory(0, bytesToWrite));
            }

            bytesRemaining -= bytesToWrite;
        }
    }
}