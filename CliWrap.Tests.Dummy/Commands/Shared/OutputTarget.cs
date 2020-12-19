using System;

namespace CliWrap.Tests.Dummy.Commands.Shared
{
    [Flags]
    public enum OutputTarget
    {
        StdOut = 1,
        StdErr = 2,
        All = StdOut | StdErr
    }
}