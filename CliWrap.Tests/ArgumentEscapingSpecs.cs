using System;
using System.Threading.Tasks;
using CliWrap.Buffered;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class ArgumentEscapingSpecs
    {
        [Theory]
        [InlineData("foo bar")]
        [InlineData("\"quoted\"")]
        [InlineData("slash\\")]
        public async Task Special_characters_in_command_line_arguments_are_escaped_properly(string argument)
        {
            // Arrange
            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("echo")
                    .Add(argument)
                    .Add("--separator").Add("<|>"));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            var receivedArguments = result.StandardOutput.Split("<|>", StringSplitOptions.RemoveEmptyEntries);
            receivedArguments.Should().ContainSingle(argument);
        }

        [Theory]
        [InlineData("foo bar", "foo")]
        [InlineData("\"quoted\"", "quoted")]
        [InlineData("\"two words\"", "two words")]
        [InlineData("\\\"foo bar\\\"", "\"foo")]
        public async Task Escaping_can_be_disabled(string argument, string expectedFirstArgument)
        {
            // https://github.com/Tyrrrz/CliWrap/issues/72

            // Arrange
            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("echo")
                    .Add(argument, false)
                    .Add("--separator").Add("<|>"));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            var receivedArguments = result.StandardOutput.Split("<|>", StringSplitOptions.RemoveEmptyEntries);
            receivedArguments.Should().StartWith(expectedFirstArgument);
        }
    }
}