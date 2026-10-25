using System;
using System.Collections.Generic;
using AwesomeAssertions;
using AwesomeAssertions.Primitives;

namespace CliWrap.Tests.Utils.Extensions;

internal static class AssertionExtensions
{
    extension(StringAssertions assertions)
    {
        public void ConsistOfLines(params IEnumerable<string> lines) =>
            assertions
                .Subject.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                .Should()
                .Equal(lines);
    }
}
