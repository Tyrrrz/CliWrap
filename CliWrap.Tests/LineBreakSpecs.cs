using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class LineBreakSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_split_the_stdout_by_newline()
    {
        // Arrange
        const string data = "Foo\nBar\nBaz";

        var stdOutLines = new List<string>();

        var cmd =
            data | Cli.Wrap(Dummy.Program.FilePath).WithArguments("echo stdin") | stdOutLines.Add;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLines.Should().Equal("Foo", "Bar", "Baz");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_split_the_stdout_by_caret_return()
    {
        // Arrange
        const string data = "Foo\rBar\rBaz";

        var stdOutLines = new List<string>();

        var cmd =
            data | Cli.Wrap(Dummy.Program.FilePath).WithArguments("echo stdin") | stdOutLines.Add;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLines.Should().Equal("Foo", "Bar", "Baz");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_split_the_stdout_by_caret_return_followed_by_newline()
    {
        // Arrange
        const string data = "Foo\r\nBar\r\nBaz";

        var stdOutLines = new List<string>();

        var cmd =
            data | Cli.Wrap(Dummy.Program.FilePath).WithArguments("echo stdin") | stdOutLines.Add;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLines.Should().Equal("Foo", "Bar", "Baz");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_split_the_stdout_by_newline_while_including_empty_lines()
    {
        // Arrange
        const string data = "Foo\r\rBar\n\nBaz";

        var stdOutLines = new List<string>();

        var cmd =
            data | Cli.Wrap(Dummy.Program.FilePath).WithArguments("echo stdin") | stdOutLines.Add;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLines.Should().Equal("Foo", "", "Bar", "", "Baz");
    }
}
