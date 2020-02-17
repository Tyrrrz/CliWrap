using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CliWrap.Tests
{
    public class CancellationSpecs
    {
        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_cancel_execution_while_it_is_in_progress()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            var cli = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.Sleep)
                    .AddArgument(10_000));
            });

            // Act
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await cli.ExecuteAsync(cts.Token));
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_cancel_execution_immediately()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var cli = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.Sleep)
                    .AddArgument(10_000));
            });

            // Act
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await cli.ExecuteAsync(cts.Token));
        }
    }
}