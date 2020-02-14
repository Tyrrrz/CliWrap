using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class BufferedSpecs
    {
        [Fact]
        public async Task I_can_execute_a_CLI_and_get_the_execution_result()
        {
            // Arrange
            const int expectedExitCode = 13;

            // Act
            var result = await Cli.Wrap("dotnet", o =>
            {
                o.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.SetExitCode)
                    .AddArgument(expectedExitCode));
            }).Buffered().ExecuteAsync();

            // Assert
            result.ExitCode.Should().Be(expectedExitCode);
            result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Fact]
        public async Task I_can_execute_a_CLI_and_get_the_stdout()
        {
            // Arrange
            const string expectedStdOut = "Hello stdout";

            // Act
            var result = await Cli.Wrap("dotnet", o =>
            {
                o.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.EchoStdOut)
                    .AddArgument(expectedStdOut));
            }).Buffered().ExecuteAsync();

            // Assert
            result.ExitCode.Should().Be(0);
            result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
            result.StandardOutput.TrimEnd().Should().Be(expectedStdOut);
            result.StandardError.TrimEnd().Should().BeEmpty();
        }

        [Fact]
        public async Task I_can_execute_a_CLI_and_get_the_stderr()
        {
            // Arrange
            const string expectedStdErr = "Hello stderr";

            // Act
            var result = await Cli.Wrap("dotnet", o =>
            {
                o.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.EchoStdErr)
                    .AddArgument(expectedStdErr));
            }).Buffered().ExecuteAsync();

            // Assert
            result.ExitCode.Should().Be(0);
            result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
            result.StandardOutput.TrimEnd().Should().BeEmpty();
            result.StandardError.TrimEnd().Should().Be(expectedStdErr);
        }

        [Fact]
        public void I_can_execute_a_CLI_and_get_the_underlying_process_ID_while_it_is_running()
        {
            // Act
            var task = Cli.Wrap("dotnet", Dummy.Program.Location)
                .Buffered()
                .ExecuteAsync();

            // Assert
            task.ProcessId.Should().NotBe(0);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_it_will_not_deadlock_on_very_large_stdout()
        {
            // Act
            await Cli.Wrap("dotnet", o =>
            {
                o.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.LoopStdOut)
                    .AddArgument(100_000));
            }).Buffered().ExecuteAsync();
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_it_will_not_deadlock_on_very_large_stderr()
        {
            // Act
            await Cli.Wrap("dotnet", o =>
            {
                o.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.LoopStdErr)
                    .AddArgument(100_000));
            }).Buffered().ExecuteAsync();
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_it_will_not_deadlock_on_very_large_stdout_and_stderr()
        {
            // Act
            await Cli.Wrap("dotnet", o =>
            {
                o.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.LoopBoth)
                    .AddArgument(100_000));
            }).Buffered().ExecuteAsync();
        }
    }
}