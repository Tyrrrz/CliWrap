using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Primitives;

namespace CliWrap.Tests.Utils.Extensions;

internal static class AssertionExtensions
{
    public static void ConsistOfLines(
        this StringAssertions assertions,
        params IEnumerable<string> lines
    ) =>
        assertions
            .Subject.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Should()
            .Equal(lines);
}
