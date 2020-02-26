using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class GeneralSpecs
    {
        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_get_the_execution_result()
        {
            // Arrange
            const int expectedExitCode = 13;

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.SetExitCode)
                    .Add(expectedExitCode))
                .WithValidation(CommandResultValidation.None);

            // Act
            var result = await cmd.ExecuteAsync();

            // Assert
            result.ExitCode.Should().Be(expectedExitCode);
            result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_get_the_underlying_process_ID_while_it_is_running()
        {
            // Arrange
            var cmd = Cli.Wrap("dotnet").WithArguments(Dummy.Program.FilePath);

            // Act
            var task = cmd.ExecuteAsync();

            // Assert
            task.ProcessId.Should().NotBe(0);
            await task;
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_with_very_large_stdout_and_stderr_and_get_the_result_without_deadlocks()
        {
            // Arrange
            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintLines)
                    .Add(100_000));

            // Act
            await cmd.ExecuteAsync();
        }
    }
}