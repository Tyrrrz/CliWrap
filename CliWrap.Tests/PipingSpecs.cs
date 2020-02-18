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
            var data = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
            await using var stream = new MemoryStream(data);

            var cli = stream | Cli
                .Wrap("dotnet")
                .SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.GetStdInSize));

            // Act
            var result = await cli.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be(data.Length.ToString(CultureInfo.InvariantCulture));
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_pipe_a_string_as_input()
        {
            // Arrange
            const string str = "Hello world";

            var cli = str | Cli
                .Wrap("dotnet")
                .SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.EchoStdIn));

            // Act
            var result = await cli.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.Should().Be(str);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_pipe_another_CLI_as_input()
        {
            // Arrange
            const int expectedSize = 1_000_000;

            var cli1 = Cli
                .Wrap("dotnet")
                .SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.Binary)
                    .AddArgument(expectedSize));

            var cli2 = Cli
                .Wrap("dotnet")
                .SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.GetStdInSize));

            var cli = cli1 | cli2;

            // Act
            var result = await cli.ExecuteBufferedAsync();
            var size = int.Parse(result.StandardOutput, CultureInfo.InvariantCulture);

            // Assert
            size.Should().Be(expectedSize);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_pipe_another_CLI_as_input_in_a_chain()
        {
            // Arrange
            var cli1 = Cli
                .Wrap("dotnet")
                .SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.EchoStdIn));

            var cli2 = Cli
                .Wrap("dotnet")
                .SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.GetStdInSize));

            var cli3 = Cli
                .Wrap("dotnet")
                .SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.EchoStdIn));

            var cli4 = Cli
                .Wrap("dotnet")
                .SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.GetStdInSize));

            var cli = "Hello world" | cli1 | cli2 | cli3 | cli4;

            // Act
            var result = await cli.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be("4");
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_pipe_its_stdout_into_a_stream()
        {
            // Arrange
            const int expectedSize = 1_000_000;

            await using var stream = new MemoryStream();

            var cli = Cli
                .Wrap("dotnet")
                .SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.Binary)
                    .AddArgument(expectedSize)) | stream;

            // Act
            await cli.ExecuteAsync();

            // Assert
            stream.Length.Should().Be(expectedSize);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_pipe_its_stdout_and_stderr_into_a_stream()
        {
            // Arrange
            await using var stdOut = new MemoryStream();
            await using var stdErr = new MemoryStream();

            var cli = Cli
                .Wrap("dotnet")
                .SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.LoopBoth)
                    .AddArgument(100)) | (stdOut, stdErr);

            // Act
            await cli.ExecuteAsync();

            // Assert
            stdOut.Length.Should().NotBe(0);
            stdErr.Length.Should().NotBe(0);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_that_expects_stdin_but_pipe_nothing_and_it_will_not_deadlock()
        {
            // Arrange
            var cli = Cli
                .Wrap("dotnet")
                .SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.GetStdInSize));

            // Act
            await cli.ExecuteAsync();
        }
    }
}