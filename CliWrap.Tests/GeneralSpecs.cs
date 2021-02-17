using System;
using System.Text;
using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.Exceptions;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class GeneralSpecs
    {
        [Fact(Timeout = 15000)]
        public async Task Command_can_be_executed_which_yields_a_result_containing_runtime_information()
        {
            // Arrange
            var cmd = Cli.Wrap("dotnet").WithArguments(Dummy.Program.FilePath);

            // Act
            var result = await cmd.ExecuteAsync();

            // Assert
            result.ExitCode.Should().Be(0);
            result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Fact(Timeout = 15000)]
        public async Task Underlying_process_ID_can_be_obtained_while_a_command_is_executing()
        {
            // Arrange
            var cmd = Cli.Wrap("dotnet").WithArguments(Dummy.Program.FilePath);

            // Act
            var task = cmd.ExecuteAsync();

            // Assert
            task.ProcessId.Should().NotBe(0);
            await task;
        }

        [Fact(Timeout = 15000)]
        public async Task Command_can_be_executed_with_a_configured_awaiter()
        {
            // Arrange
            var cmd = Cli.Wrap("dotnet").WithArguments(Dummy.Program.FilePath);

            // Act
            var result = await cmd.ExecuteAsync().ConfigureAwait(false);

            // Assert
            result.ExitCode.Should().Be(0);
        }

        [Fact(Timeout = 15000)]
        public async Task Command_execution_does_not_deadlock_on_large_stdout_and_stderr()
        {
            // Arrange
            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("generate-text")
                    .Add("--target").Add("all")
                    .Add("--length").Add(100_000));

            // Act
            await cmd.ExecuteAsync();
        }

        [Fact(Timeout = 15000)]
        public async Task Command_with_non_zero_exit_code_yields_exit_code_in_exception()
        {
            // Arrange
          var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("exit-with")
                    .Add("--code").Add(1));

            // Act
            var task = cmd.ExecuteAsync();

            // Assert
            var ex = await Assert.ThrowsAsync<CommandExecutionException>(async () => await task);
            ex.ExitCode.Should().Be(1);
        }
    }
}
