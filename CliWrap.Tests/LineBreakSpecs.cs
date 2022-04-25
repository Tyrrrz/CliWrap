using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class LineBreakSpecs
{
    [Fact(Timeout = 15000)]
    public async Task Newline_char_is_treated_as_a_line_break()
    {
        // Arrange
        const string data = "Foo\nBar\nBaz";

        var stdOutLines = new List<string>();

        var cmd = data | Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo stdin")
            ) | stdOutLines.Add;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLines.Should().Equal(
            "Foo",
            "Bar",
            "Baz"
        );
    }

    [Fact(Timeout = 15000)]
    public async Task Caret_return_char_is_treated_as_a_line_break()
    {
        // Arrange
        const string data = "Foo\rBar\rBaz";

        var stdOutLines = new List<string>();

        var cmd = data | Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo stdin")
            ) | stdOutLines.Add;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLines.Should().Equal(
            "Foo",
            "Bar",
            "Baz"
        );
    }

    [Fact(Timeout = 15000)]
    public async Task Caret_return_char_followed_by_newline_char_is_treated_as_a_single_line_break()
    {
        // Arrange
        const string data = "Foo\r\nBar\r\nBaz";

        var stdOutLines = new List<string>();

        var cmd = data | Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo stdin")
            ) | stdOutLines.Add;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLines.Should().Equal(
            "Foo",
            "Bar",
            "Baz"
        );
    }

    [Fact(Timeout = 15000)]
    public async Task Multiple_consecutive_line_breaks_are_treated_as_separate_line_breaks()
    {
        // Arrange
        const string data = "Foo\r\rBar\n\nBaz";

        var stdOutLines = new List<string>();

        var cmd = data | Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo stdin")
            ) | stdOutLines.Add;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLines.Should().Equal(
            "Foo",
            "",
            "Bar",
            "",
            "Baz"
        );
    }
}