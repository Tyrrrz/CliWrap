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
        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_pipe_a_stream_as_input()
        {
            // Arrange
            const string expectedOutput = "Hello world";

            await using var stream = new MemoryStream();
            var data = Encoding.ASCII.GetBytes(expectedOutput);
            await stream.WriteAsync(data, 0, data.Length);
            stream.Seek(0, SeekOrigin.Begin);

            var cli = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.EchoStdIn));
            });

            // Act
            var result = await (stream | cli).Buffered().ExecuteAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be(expectedOutput);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_pipe_a_string_as_input()
        {
            // Arrange
            const string expectedOutput = "Hello world";

            var cli = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.EchoStdIn));
            });

            // Act
            var result = await (expectedOutput | cli).Buffered().ExecuteAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be(expectedOutput);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_pipe_another_CLI_as_input()
        {
            // Arrange
            const int expectedSize = 1_000_000;

            var cli1 = Cli.Wrap("dotnet", c =>
            {
                // This produces random binary output to stdout
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.Binary)
                    .AddArgument(expectedSize));
            });

            var cli2 = Cli.Wrap("dotnet", c =>
            {
                // This counts the number of bytes in stdin
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.GetStdInSize));
            });

            // Act
            var result = await (cli1 | cli2).Buffered().ExecuteAsync();
            var size = int.Parse(result.StandardOutput, CultureInfo.InvariantCulture);

            // Assert
            size.Should().Be(expectedSize);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_pipe_another_CLI_as_input_in_a_chain()
        {
            // Arrange
            var cli1 = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.EchoStdIn));
            });

            var cli2 = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.GetStdInSize));
            });

            var cli3 = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.EchoStdIn));
            });

            var cli4 = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.GetStdInSize));
            });

            // Act
            var result = await ("Hello world" | cli1 | cli2 | cli3 | cli4).Buffered().ExecuteAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be("2");
        }

        [Fact(Skip = "Pending implementation")]
        public async Task I_can_execute_a_CLI_and_pipe_its_output_into_a_stream()
        {
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_that_expects_stdin_but_pipe_nothing_and_it_will_not_deadlock()
        {
            // Arrange
            var cli = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.GetStdInSize));
            });

            // Act
            await cli.ExecuteAsync();
        }
    }
}