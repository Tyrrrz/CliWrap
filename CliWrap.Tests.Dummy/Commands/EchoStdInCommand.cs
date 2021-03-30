using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using System;
using CliFx.Infrastructure;
using CliWrap.Tests.Dummy.Commands.Shared;

namespace CliWrap.Tests.Dummy.Commands
{
    [Command("echo-stdin")]
    public class EchoStdInCommand : ICommand
    {
        [CommandOption("target")]
        public OutputTarget Target { get; init; } = OutputTarget.StdOut;

        [CommandOption("length")]
        public long? Length { get; init; }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            var buffer = new byte[81920];
            var bytesCopied = 0L;

            while (Length is null || bytesCopied < Length)
            {
                var bytesToRead = Length is { } length
                    ? (int) Math.Min(buffer.Length, length - bytesCopied)
                    : buffer.Length;

                var bytesRead = await console.Input.BaseStream.ReadAsync(buffer.AsMemory(0, bytesToRead));
                if (bytesRead <= 0)
                    break;

                if (Target.HasFlag(OutputTarget.StdOut))
                {
                    await console.Output.BaseStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                }

                if (Target.HasFlag(OutputTarget.StdErr))
                {
                    await console.Error.BaseStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                }

                bytesCopied += bytesRead;
            }
        }
    }
}