using System;
using System.Collections.Generic;
using CliFx.Infrastructure;

namespace CliWrap.Tests.Dummy.Commands.Shared;

[Flags]
public enum OutputTarget
{
    StdOut = 1,
    StdErr = 2,
    All = StdOut | StdErr,
}

internal static class OutputTargetExtensions
{
    public static IEnumerable<ConsoleWriter> GetWriters(this IConsole console, OutputTarget target)
    {
        if (target.HasFlag(OutputTarget.StdOut))
            yield return console.Output;

        if (target.HasFlag(OutputTarget.StdErr))
            yield return console.Error;
    }
}
