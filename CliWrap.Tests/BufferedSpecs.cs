using System;
using System.Threading.Tasks;
using CliWrap.Buffered;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class BufferedSpecs
    {
        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_get_the_result_which_contains_buffered_stdout()
        {
            // Arrange
            const string expectedStdOut = "Hello stdout";

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.EchoArgsToStdOut)
                    .Add(expectedStdOut));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.ExitCode.Should().Be(0);
            result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
            result.StandardOutput.TrimEnd().Should().Be(expectedStdOut);
            result.StandardError.TrimEnd().Should().BeEmpty();
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_get_the_result_which_contains_buffered_stderr()
        {
            // Arrange
            const string expectedStdErr = "Hello stderr";

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.EchoArgsToStdErr)
                    .Add(expectedStdErr));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.ExitCode.Should().Be(0);
            result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
            result.StandardOutput.TrimEnd().Should().BeEmpty();
            result.StandardError.TrimEnd().Should().Be(expectedStdErr);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_get_the_result_which_contains_buffered_stdout_and_stderr()
        {
            // Arrange
            const string expectedStdOut = "Hello stdout";

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.EchoArgsToStdOutAndStdErr)
                    .Add(expectedStdOut));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.ExitCode.Should().Be(0);
            result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
            result.StandardOutput.TrimEnd().Should().Be(expectedStdOut);
            result.StandardError.TrimEnd().Should().Be(expectedStdOut);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_with_very_large_stdout_and_stderr_and_get_the_buffered_result_without_deadlocks()
        {
            // Arrange
            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintLines)
                    .Add(10_000));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.Should().NotBeNullOrWhiteSpace();
            result.StandardError.Should().NotBeNullOrWhiteSpace();
        }
    }
}