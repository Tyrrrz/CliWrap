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
        public async Task I_can_specify_command_line_argument_that_contains_special_characters_and_it_will_be_escaped_properly(
            string argument)
        {
            // Arrange
            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.EchoFirstArgToStdOut)
                    .Add(argument));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be(argument);
        }
    }
}