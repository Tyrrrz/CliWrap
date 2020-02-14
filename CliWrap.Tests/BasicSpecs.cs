using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class BasicSpecs
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
                    .AddArgument(Dummy.Program.FilePath)
                    .AddArgument(Dummy.Program.SetExitCode)
                    .AddArgument(expectedExitCode));
            }).ExecuteAsync();

            // Assert
            result.ExitCode.Should().Be(expectedExitCode);
            result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Fact]
        public void I_can_execute_a_CLI_and_get_the_underlying_process_ID_while_it_is_running()
        {
            // Act
            var task = Cli.Wrap("dotnet", Dummy.Program.FilePath).ExecuteAsync();

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
                    .AddArgument(Dummy.Program.FilePath)
                    .AddArgument(Dummy.Program.LoopStdOut)
                    .AddArgument(100_000));
            }).ExecuteAsync();
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_it_will_not_deadlock_on_very_large_stderr()
        {
            // Act
            await Cli.Wrap("dotnet", o =>
            {
                o.SetArguments(a => a
                    .AddArgument(Dummy.Program.FilePath)
                    .AddArgument(Dummy.Program.LoopStdErr)
                    .AddArgument(100_000));
            }).ExecuteAsync();
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_it_will_not_deadlock_on_very_large_stdout_and_stderr()
        {
            // Act
            await Cli.Wrap("dotnet", o =>
            {
                o.SetArguments(a => a
                    .AddArgument(Dummy.Program.FilePath)
                    .AddArgument(Dummy.Program.LoopBoth)
                    .AddArgument(100_000));
            }).ExecuteAsync();
        }
    }
}