using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliWrap.Tests.Dummy.Commands.Shared;

namespace CliWrap.Tests.Dummy.Commands
{
    [Command("echo")]
    public class EchoCommand : ICommand
    {
        [CommandParameter(0)]
        public IReadOnlyList<string> Items { get; init; } = Array.Empty<string>();

        [CommandOption("target")]
        public OutputTarget Target { get; init; } = OutputTarget.StdOut;

        [CommandOption("separator")]
        public string Separator { get; init; } = " ";

        public async ValueTask ExecuteAsync(IConsole console)
        {
            var joined = string.Join(Separator, Items);

            if (Target.HasFlag(OutputTarget.StdOut))
            {
                await console.Output.WriteAsync(joined);
            }

            if (Target.HasFlag(OutputTarget.StdErr))
            {
                await console.Error.WriteAsync(joined);
            }
        }
    }
}