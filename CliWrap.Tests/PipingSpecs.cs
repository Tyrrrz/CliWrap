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

            var cmd = stream | Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintStdInLength));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be(stream.Length.ToString(CultureInfo.InvariantCulture));
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_pipe_a_file_into_stdin()
        {
            // Arrange
            const string expectedContent = "Hell world!";
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, expectedContent);

            await using var fileStream = File.OpenRead(filePath);

            var cmd = fileStream | Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.EchoStdInToStdOut));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be(expectedContent);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_pipe_a_string_into_stdin()
        {
            // Arrange
            const string str = "Hello world";

            var cmd = str | Cli.Wrap("dotnet")
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
                    .Add(Dummy.Program.EchoPartStdInToStdOut)
                    .Add(5)) |
                Cli.Wrap("dotnet").WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintStdInLength));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be("5");
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_pipe_its_stdout_into_a_stream()
        {
            // Arrange
            const int expectedSize = 1_000_000;
            await using var stream = new MemoryStream();

            var cmd = Cli.Wrap("dotnet")
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
        public async Task I_can_execute_a_command_and_pipe_its_stdout_into_a_file()
        {
            // Arrange
            const int expectedSize = 1_000_000;
            var file = new FileInfo(Path.GetTempFileName());
            var fileStream = file.Create();

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.ProduceBinary)
                    .Add(expectedSize)) | fileStream;

            // Act
            await cmd.ExecuteAsync();
            await fileStream.DisposeAsync();

            // Assert
            file.Exists.Should().Be(true);
            file.Length.Should().Be(expectedSize);
            file.Delete();
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_pipe_its_stdout_into_a_delegate()
        {
            // Arrange
            const int expectedLinesCount = 100;

            var stdOutLinesCount = 0;

            void HandleStdOut(string s) => stdOutLinesCount++;

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintLines)
                    .Add(expectedLinesCount)) | HandleStdOut;

            // Act
            await cmd.ExecuteAsync();

            // Assert
            stdOutLinesCount.Should().Be(expectedLinesCount);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_pipe_its_stdout_and_stderr_into_separate_streams()
        {
            // Arrange
            await using var stdOut = new MemoryStream();
            await using var stdErr = new MemoryStream();

            var cmd = Cli.Wrap("dotnet")
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
        public async Task I_can_execute_a_command_and_pipe_its_stdout_and_stderr_into_separate_delegates()
        {
            // Arrange
            const int expectedLinesCount = 100;

            var stdOutLinesCount = 0;
            var stdErrLinesCount = 0;

            void HandleStdOut(string s) => stdOutLinesCount++;
            void HandleStdErr(string s) => stdErrLinesCount++;

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintLines)
                    .Add(expectedLinesCount)) | (HandleStdOut, HandleStdErr);

            // Act
            await cmd.ExecuteAsync();

            // Assert
            stdOutLinesCount.Should().Be(expectedLinesCount);
            stdErrLinesCount.Should().Be(expectedLinesCount);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_pipe_its_stdout_into_multiple_streams()
        {
            // Arrange
            const int expectedSize = 1_000_000;
            await using var stream1 = new MemoryStream();
            await using var stream2 = new MemoryStream();
            await using var stream3 = new MemoryStream();

            var pipeTarget = PipeTarget.Merge(
                PipeTarget.ToStream(stream1),
                PipeTarget.ToStream(stream2),
                PipeTarget.ToStream(stream3));

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.ProduceBinary)
                    .Add(expectedSize)) | pipeTarget;

            // Act
            await cmd.ExecuteAsync();

            // Assert
            stream1.Length.Should().Be(expectedSize);
            stream2.Length.Should().Be(expectedSize);
            stream3.Length.Should().Be(expectedSize);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_pipe_its_stdout_into_a_stream_while_also_buffering()
        {
            // Arrange
            const string expectedContent = "Hello world!";
            await using var stream = new MemoryStream();

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.EchoArgsToStdOut)
                    .Add(expectedContent)) | stream;

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            stream.Seek(0, SeekOrigin.Begin);
            using var streamReader = new StreamReader(stream);
            var streamContent = await streamReader.ReadToEndAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be(expectedContent);
            streamContent.TrimEnd().Should().Be(expectedContent);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_that_expects_stdin_but_pipe_nothing_and_it_will_not_deadlock()
        {
            // Arrange
            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintStdInLength));

            // Act
            await cmd.ExecuteAsync();
        }
    }
}