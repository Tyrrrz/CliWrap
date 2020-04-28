using System.IO;
using System.Text;
using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.Tests.Internal;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class PipingSpecs
    {
        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_that_pipes_stdin_from_a_stream()
        {
            // Arrange
            await using var stream = new MemoryStream(new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9});

            var cmd = stream | Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintStdInLength));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be("10");
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_that_pipes_stdin_from_a_file()
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
        public async Task I_can_execute_a_command_that_pipes_stdin_from_a_string()
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
        public async Task I_can_execute_a_command_that_pipes_its_stdin_from_stdout_of_another_command()
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
            result.StandardOutput.TrimEnd().Should().Be("1000000");
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_that_represents_a_pipeline_of_multiple_commands()
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
        public async Task I_can_execute_a_command_that_pipes_its_stdout_into_a_stream()
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
        public async Task I_can_execute_a_command_that_pipes_its_stdout_into_a_file()
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
        public async Task I_can_execute_a_command_that_pipes_its_stdout_into_a_string_builder()
        {
            // Arrange
            const string expectedOutput = "Hello world!";
            var buffer = new StringBuilder();

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.EchoArgsToStdOut)
                    .Add(expectedOutput)) | buffer;

            // Act
            await cmd.ExecuteAsync();

            // Assert
            buffer.ToString().TrimEnd().Should().Be(expectedOutput);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_that_pipes_its_stdout_into_a_delegate()
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
        public async Task I_can_execute_a_command_that_pipes_its_stdout_into_an_async_delegate()
        {
            // Arrange
            const int expectedLinesCount = 100;

            var stdOutLinesCount = 0;

            async Task HandleStdOutAsync(string s)
            {
                await Task.Yield();
                stdOutLinesCount++;
            }

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintLines)
                    .Add(expectedLinesCount)) | HandleStdOutAsync;

            // Act
            await cmd.ExecuteAsync();

            // Assert
            stdOutLinesCount.Should().Be(expectedLinesCount);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_that_pipes_its_stdout_and_stderr_into_separate_streams()
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
        public async Task I_can_execute_a_command_that_pipes_its_stdout_and_stderr_into_separate_string_builders()
        {
            // Arrange
            const string expectedOutput = "Hello world!";

            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.EchoArgsToStdOutAndStdErr)
                    .Add(expectedOutput)) | (stdOutBuffer, stdErrBuffer);

            // Act
            await cmd.ExecuteAsync();

            // Assert
            stdOutBuffer.ToString().TrimEnd().Should().Be(expectedOutput);
            stdErrBuffer.ToString().TrimEnd().Should().Be(expectedOutput);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_that_pipes_its_stdout_and_stderr_into_separate_delegates()
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
        public async Task I_can_execute_a_command_that_pipes_its_stdout_and_stderr_into_separate_async_delegates()
        {
            // Arrange
            const int expectedLinesCount = 100;

            var stdOutLinesCount = 0;
            var stdErrLinesCount = 0;

            async Task HandleStdOutAsync(string s)
            {
                await Task.Yield();
                stdOutLinesCount++;
            }

            async Task HandleStdErrAsync(string s)
            {
                await Task.Yield();
                stdErrLinesCount++;
            }

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintLines)
                    .Add(expectedLinesCount)) | (HandleStdOutAsync, HandleStdErrAsync);

            // Act
            await cmd.ExecuteAsync();

            // Assert
            stdOutLinesCount.Should().Be(expectedLinesCount);
            stdErrLinesCount.Should().Be(expectedLinesCount);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_that_pipes_its_stdout_into_multiple_streams()
        {
            // Arrange
            const int expectedSize = 1_000_000;
            await using var stream1 = new MemoryStream();
            await using var stream2 = new MemoryStream();
            await using var stream3 = new MemoryStream();

            var pipeTarget = PipeTarget.Merge(
                PipeTarget.ToStream(stream1),
                PipeTarget.ToStream(stream2),
                PipeTarget.ToStream(stream3)
            );

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
        public async Task I_can_execute_a_command_that_pipes_its_stdout_into_a_stream_while_also_buffering()
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

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_pipe_a_really_large_stdin_while_it_also_writes_stdout_and_it_will_not_deadlock()
        {
            // Arrange
            await using var stream = new RandomStream(10_000_000);

            var cmd = stream | Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.EchoStdInToStdOut));

            // Act
            await cmd.ExecuteAsync();
        }
    }
}