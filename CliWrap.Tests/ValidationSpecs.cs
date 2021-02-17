using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.Exceptions;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace CliWrap.Tests
{
    public class ValidationSpecs
    {
        private readonly ITestOutputHelper _output;

        public ValidationSpecs(ITestOutputHelper output) => _output = output;

        [Fact(Timeout = 15000)]
        public async Task Command_execution_throws_if_the_process_exits_with_a_non_zero_exit_code()
        {
            // Arrange
            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("exit-with")
                    .Add("--code").Add(-1));

            // Act & assert
            var ex = await Assert.ThrowsAsync<CommandExecutionException>(() => cmd.ExecuteAsync());
            _output.WriteLine(ex.Message);
        }

        [Fact(Timeout = 15000)]
        public async Task Buffered_command_execution_throws_a_detailed_error_if_the_process_exits_with_a_non_zero_exit_code()
        {
            // Arrange
            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("exit-with")
                    .Add("--code").Add(-1));

            // Act & assert
            var ex = await Assert.ThrowsAsync<CommandExecutionException>(() => cmd.ExecuteBufferedAsync());
            _output.WriteLine(ex.Message);
        }

        [Fact(Timeout = 15000)]
        public async Task Exit_code_validation_can_be_disabled()
        {
            // Arrange
            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("exit-with")
                    .Add("--code").Add(-1))
                .WithValidation(CommandResultValidation.None);

            // Act & assert
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