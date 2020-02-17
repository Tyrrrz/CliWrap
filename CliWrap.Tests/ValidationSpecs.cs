using System.Threading.Tasks;
using CliWrap.Exceptions;
using CliWrap.Tests.Internal;
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
            var cli = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.SetExitCode)
                    .AddArgument(-1));
            });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<CliExecutionException>(async () => await cli.ExecuteAsync());
            _output.WriteLine(ex.Message);
        }

        [Fact]
        public async Task I_can_execute_a_CLI_as_buffered_and_get_an_exception_if_it_returns_a_non_zero_exit_code()
        {
            // Arrange
            var cli = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.SetExitCode)
                    .AddArgument(-1));
            }).Buffered();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<CliExecutionException>(async () => await cli.ExecuteAsync());
            _output.WriteLine(ex.Message);
        }

        [Fact]
        public async Task I_can_execute_a_CLI_as_streaming_and_get_an_exception_if_it_returns_a_non_zero_exit_code()
        {
            // Arrange
            var cli = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.SetExitCode)
                    .AddArgument(-1));
            }).Streaming();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<CliExecutionException>(async () => await cli.ExecuteAsync().AggregateAsync());
            _output.WriteLine(ex.Message);
        }
    }
}