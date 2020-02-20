using System.Globalization;
using System.IO;
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

            var cmd = stream | Cli
                .Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.Location)
                    .Add(Dummy.Program.GetStdInSize));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be(data.Length.ToString(CultureInfo.InvariantCulture));
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_pipe_a_string_as_input()
        {
            // Arrange
            const string str = "Hello world";

            var cmd = str | Cli
                .Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.Location)
                    .Add(Dummy.Program.EchoStdIn));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.Should().Be(str);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_pipe_another_CLI_as_input()
        {
            // Arrange
            const int expectedSize = 1_000_000;

            var cmd =
                Cli.Wrap("dotnet").WithArguments(a => a.Add(Dummy.Program.Location).Add(Dummy.Program.Binary).Add(expectedSize)) |
                Cli.Wrap("dotnet").WithArguments(a => a.Add(Dummy.Program.Location).Add(Dummy.Program.GetStdInSize));

            // Act
            var result = await cmd.ExecuteBufferedAsync();
            var size = int.Parse(result.StandardOutput, CultureInfo.InvariantCulture);

            // Assert
            size.Should().Be(expectedSize);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_pipe_a_chain_of_CLIs_into_its_stdin()
        {
            // Arrange
            var cmd =
                "Hello world" |
                Cli.Wrap("dotnet").WithArguments(a => a.Add(Dummy.Program.Location).Add(Dummy.Program.EchoStdIn)) |
                Cli.Wrap("dotnet").WithArguments(a => a.Add(Dummy.Program.Location).Add(Dummy.Program.GetStdInSize)) |
                Cli.Wrap("dotnet").WithArguments(a => a.Add(Dummy.Program.Location).Add(Dummy.Program.EchoStdIn)) |
                Cli.Wrap("dotnet").WithArguments(a => a.Add(Dummy.Program.Location).Add(Dummy.Program.GetStdInSize));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be("4");
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_pipe_its_stdout_into_a_stream()
        {
            // Arrange
            const int expectedSize = 1_000_000;

            await using var stream = new MemoryStream();

            var cmd = Cli
                .Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.Location)
                    .Add(Dummy.Program.Binary)
                    .Add(expectedSize)) | stream;

            // Act
            await cmd.ExecuteAsync();

            // Assert
            stream.Length.Should().Be(expectedSize);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_pipe_its_stdout_and_stderr_into_separate_streams()
        {
            // Arrange
            await using var stdOut = new MemoryStream();
            await using var stdErr = new MemoryStream();

            var cmd = Cli
                .Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.Location)
                    .Add(Dummy.Program.LoopBoth)
                    .Add(100)) | (stdOut, stdErr);

            // Act
            await cmd.ExecuteAsync();

            // Assert
            stdOut.Length.Should().NotBe(0);
            stdErr.Length.Should().NotBe(0);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_that_expects_stdin_but_pipe_nothing_and_it_will_not_deadlock()
        {
            // Arrange
            var cmd = Cli
                .Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.Location)
                    .Add(Dummy.Program.GetStdInSize));

            // Act
            await cmd.ExecuteAsync();
        }
    }
}