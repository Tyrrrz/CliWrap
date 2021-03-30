using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using CliWrap.Tests.Dummy.Commands.Shared;

namespace CliWrap.Tests.Dummy.Commands
{
    [Command("generate-text")]
    public class GenerateTextCommand : ICommand
    {
        // Tests rely on the random seed being fixed
        private readonly Random _random = new(1234567);
        private readonly char[] _allowedChars = Enumerable.Range(32, 94).Select(i => (char) i).ToArray();

        [CommandOption("target")]
        public OutputTarget Target { get; init; } = OutputTarget.StdOut;

        [CommandOption("length")]
        public int Length { get; init; } = 1000;

        public async ValueTask ExecuteAsync(IConsole console)
        {
            var buffer = new StringBuilder(Length);
            for (var i = 0; i < Length; i++)
            {
                buffer.Append(
                    _allowedChars[_random.Next(0, _allowedChars.Length)]
                );
            }

            var text = buffer.ToString();

            if (Target.HasFlag(OutputTarget.StdOut))
            {
                await console.Output.WriteAsync(text);
            }

            if (Target.HasFlag(OutputTarget.StdErr))
            {
                await console.Error.WriteAsync(text);
            }
        }
    }
}