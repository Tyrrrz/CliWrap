using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CliWrap.Buffered;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class PipingSpecs
    {
        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_pipe_a_stream_into_stdin()
        {
            // Arrange
            await using var stream = File.OpenRead(typeof(PipingSpecs).Assembly.Location);

            var cmd = stream | Cli
                .Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintStdInLength));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be(stream.Length.ToString(CultureInfo.InvariantCulture));
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_pipe_a_string_into_stdin()
        {
            // Arrange
            const string str = "Hello world";

            var cmd = str | Cli
                .Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.EchoStdInToStdOut));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.Should().Be(str);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_pipe_another_command_into_stdin()
        {
            // Arrange
            const int expectedSize = 1_000_000;

            var cmd =
                Cli.Wrap("dotnet").WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.ProduceBinary)
                    .Add(expectedSize)) |
                Cli.Wrap("dotnet").WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintStdInLength));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be(expectedSize.ToString(CultureInfo.InvariantCulture));
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_pipe_a_chain_of_commands_into_stdin()
        {
            // Arrange
            var cmd =
                "Hello world" |
                Cli.Wrap("dotnet").WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.EchoStdInToStdOut)) |
                Cli.Wrap("dotnet").WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintStdInLength)) |
                Cli.Wrap("dotnet").WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.EchoStdInToStdOut)) |
                Cli.Wrap("dotnet").WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintStdInLength));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be((2 + Environment.NewLine.Length).ToString());
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_pipe_its_stdout_into_a_stream()
        {
            // Arrange
            const int expectedSize = 1_000_000;
            await using var stream = new MemoryStream();

            var cmd = Cli
                .Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.ProduceBinary)
                    .Add(expectedSize)) | stream;

            // Act
            await cmd.ExecuteAsync();

            // Assert
            stream.Length.Should().Be(expectedSize);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_pipe_its_stdout_and_stderr_into_separate_streams()
        {
            // Arrange
            await using var stdOut = new MemoryStream();
            await using var stdErr = new MemoryStream();

            var cmd = Cli
                .Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintLines)
                    .Add(100)) | (stdOut, stdErr);

            // Act
            await cmd.ExecuteAsync();

            // Assert
            stdOut.Length.Should().NotBe(0);
            stdErr.Length.Should().NotBe(0);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_that_expects_stdin_but_pipe_nothing_and_it_will_not_deadlock()
        {
            // Arrange
            var cmd = Cli
                .Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintStdInLength));

            // Act
            await cmd.ExecuteAsync();
        }
    }
}