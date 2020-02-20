using System.Threading.Tasks;
using CliWrap.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace CliWrap.Tests
{
    public class ValidationSpecs
    {
        private readonly ITestOutputHelper _output;

        public ValidationSpecs(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task I_can_execute_a_CLI_and_get_an_exception_if_it_returns_a_non_zero_exit_code()
        {
            // Arrange
            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.Location)
                    .Add(Dummy.Program.SetExitCode)
                    .Add(-1));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<CommandExecutionException>(async () => await cmd.ExecuteAsync());
            _output.WriteLine(ex.Message);
        }

        [Fact]
        public async Task I_can_execute_a_CLI_as_buffered_and_get_an_exception_if_it_returns_a_non_zero_exit_code()
        {
            // Arrange
            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.Location)
                    .Add(Dummy.Program.SetExitCode)
                    .Add(-1));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<CommandExecutionException>(async () => await cmd.ExecuteBufferedAsync());
            _output.WriteLine(ex.Message);
        }
    }
}