using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using CliWrap.Tests.Dummy.Commands.Shared;

namespace CliWrap.Tests.Dummy.Commands;

[Command("generate text")]
public partial class GenerateTextCommand : ICommand
{
    // Tests rely on the random seed being fixed
    private readonly Random _random = new(1234567);
    private readonly char[] _allowedChars = Enumerable.Range(32, 94).Select(i => (char)i).ToArray();

    [CommandOption("target")]
    public OutputTarget Target { get; set; } = OutputTarget.StdOut;

    [CommandOption("length")]
    public int Length { get; set; } = 100_000;

    [CommandOption("lines")]
    public int LinesCount { get; set; } = 1;

    private int LineLength => LinesCount > 0 ? Length / LinesCount : 0;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (LineLength <= 0)
            return;

        for (var line = 0; line < LinesCount; line++)
        {
            var currentLineLength =
                line < LinesCount - 1
                    ? LineLength
                    // Place any remaining characters in the last line so that the total output length
                    // is always equal to Length.
                    : Length - LineLength * line;

            var buffer = new StringBuilder(currentLineLength);

            for (var i = 0; i < currentLineLength; i++)
            {
                buffer.Append(_allowedChars[_random.Next(0, _allowedChars.Length)]);
            }

            foreach (var writer in console.GetWriters(Target))
                await writer.WriteLineAsync(buffer.ToString());
        }
    }
}
