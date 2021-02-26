using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class LineBreakSpecs
    {
        [Fact]
        public async Task Newline_char_is_treated_as_a_line_break()
        {
            // Arrange
            const string content = "Foo\nBar\nBaz";

            var stdOutLines = new List<string>();

            var cmd = content | Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("echo-stdin")) | stdOutLines.Add;

            // Act
            await cmd.ExecuteAsync();

            // Assert
            stdOutLines.Should().Equal(
                "Foo",
                "Bar",
                "Baz"
            );
        }

        [Fact]
        public async Task Caret_return_char_is_treated_as_a_line_break()
        {
            // Arrange
            const string content = "Foo\rBar\rBaz";

            var stdOutLines = new List<string>();

            var cmd = content | Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("echo-stdin")) | stdOutLines.Add;

            // Act
            await cmd.ExecuteAsync();

            // Assert
            stdOutLines.Should().Equal(
                "Foo",
                "Bar",
                "Baz"
            );
        }

        [Fact]
        public async Task Caret_return_char_followed_by_newline_char_is_treated_as_a_single_line_break()
        {
            // Arrange
            const string content = "Foo\r\nBar\r\nBaz";

            var stdOutLines = new List<string>();

            var cmd = content | Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("echo-stdin")) | stdOutLines.Add;

            // Act
            await cmd.ExecuteAsync();

            // Assert
            stdOutLines.Should().Equal(
                "Foo",
                "Bar",
                "Baz"
            );
        }

        [Fact]
        public async Task Multiple_consecutive_line_breaks_are_treated_as_separate_line_breaks()
        {
            // Arrange
            const string content = "Foo\r\rBar\n\nBaz";

            var stdOutLines = new List<string>();

            var cmd = content | Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("echo-stdin")) | stdOutLines.Add;

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
}