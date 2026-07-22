using System;
using System.Linq;
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
    private static readonly char[] AllowedChars = Enumerable
        .Range(32, 94)
        .Select(i => (char)i)
        .ToArray();

    [CommandOption("target")]
    public OutputTarget Target { get; set; } = OutputTarget.StdOut;

    [CommandOption("length")]
    public int Length { get; set; } = 100_000;

    [CommandOption("lines")]
    public int LinesCount { get; set; } = 1;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (Length <= 0 || LinesCount <= 0)
            return;

        var lineLength = LinesCount > 0 ? Length / LinesCount : 0;

        for (var lineNumber = 0; lineNumber < LinesCount; lineNumber++)
        {
            var currentLineLength =
                lineNumber < LinesCount - 1
                    ? lineLength
                    // Place any remaining characters in the last line so that the total output length
                    // is always equal to Length.
                    : Length - lineLength * lineNumber;

            var line = string.Create(
                currentLineLength,
                _random,
                (buffer, random) => random.GetItems(AllowedChars, buffer)
            );

            foreach (var writer in console.GetWriters(Target))
                await writer.WriteLineAsync(line);
        }
    }
}
