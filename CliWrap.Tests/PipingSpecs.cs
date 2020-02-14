using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class PipingSpecs
    {
        [Fact]
        public async Task I_can_execute_a_CLI_and_pipe_a_stream_as_input()
        {
            // Arrange
            const string expectedOutput = "Hello world";

            await using var stream = new MemoryStream();
            var data = Encoding.ASCII.GetBytes(expectedOutput);
            await stream.WriteAsync(data, 0, data.Length);
            stream.Seek(0, SeekOrigin.Begin);

            // Act
            var result = await Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.FilePath)
                    .AddArgument(Dummy.Program.EchoStdIn));
            }).PipeFrom(stream).Buffered().ExecuteAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be(expectedOutput);
        }

        [Fact]
        public async Task I_can_execute_a_CLI_and_pipe_a_string_as_input()
        {
            // Arrange
            const string expectedOutput = "Hello world";

            // Act
            var result = await Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.FilePath)
                    .AddArgument(Dummy.Program.EchoStdIn));
            }).PipeFrom(expectedOutput).Buffered().ExecuteAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be(expectedOutput);
        }

        [Fact]
        public async Task I_can_execute_a_CLI_and_pipe_its_stdout_into_another_CLI()
        {
            // Arrange
            const long bytes = 1_000_000;

            // Act
            var result = await Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.FilePath)
                    .AddArgument(Dummy.Program.GetSize));
            }).PipeFrom(Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.FilePath)
                    .AddArgument(Dummy.Program.Binary)
                    .AddArgument(bytes));
            })).Buffered().ExecuteAsync();

            var size = long.Parse(result.StandardOutput, CultureInfo.InvariantCulture);

            // Assert
            size.Should().Be(bytes);
        }

        [Fact]
        public async Task I_can_execute_a_CLI_and_pipe_its_stdout_into_another_CLI_using_operators()
        {
            // Arrange
            const long bytes = 1_000_000;

            // Act
            var cli1 = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.FilePath)
                    .AddArgument(Dummy.Program.Binary)
                    .AddArgument(bytes));
            });

            var cli2 = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.FilePath)
                    .AddArgument(Dummy.Program.GetSize));
            });

            var result = await (cli1 > cli2).Buffered().ExecuteAsync();

            var size = long.Parse(result.StandardOutput, CultureInfo.InvariantCulture);

            // Assert
            size.Should().Be(bytes);
        }
    }
}