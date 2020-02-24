using System.Threading.Tasks;
using CliWrap.Buffered;
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

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_get_an_exception_if_it_returns_a_non_zero_exit_code()
        {
            // Arrange
            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.SetExitCode)
                    .Add(-1));

            // Act & assert
            var ex = await Assert.ThrowsAsync<CommandExecutionException>(() => cmd.ExecuteAsync());
            _output.WriteLine(ex.Message);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_with_buffering_and_get_an_exception_that_contains_stderr_if_it_returns_a_non_zero_exit_code()
        {
            // Arrange
            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.SetExitCode)
                    .Add(-1));

            // Act & assert
            var ex = await Assert.ThrowsAsync<CommandExecutionException>(() => cmd.ExecuteBufferedAsync());
            _output.WriteLine(ex.Message);
        }
    }
}