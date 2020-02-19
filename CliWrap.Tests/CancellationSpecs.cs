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
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));

            var cli = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.Location)
                    .Add(Dummy.Program.Sleep)
                    .Add(10_000));

            // Act
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await cli.ExecuteAsync(cts.Token));
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_cancel_execution_immediately()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var cli = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.Location)
                    .Add(Dummy.Program.Sleep)
                    .Add(10_000));

            // Act
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await cli.ExecuteAsync(cts.Token));
        }

        /*
        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_as_streaming_and_cancel_execution_while_it_is_in_progress()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));

            var cli = Cli.Wrap("dotnet")
                .SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.LoopBoth)
                    .AddArgument(10_000_000));

            // Act
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await cli.StartEventStreamAsync().WithCancellation(cts.Token).AggregateAsync());
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_as_streaming_and_cancel_execution_immediately()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var cli = Cli.Wrap("dotnet")
                .SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.LoopBoth)
                    .AddArgument(100_000_000));

            // Act
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await cli.StartEventStreamAsync().WithCancellation(cts.Token).AggregateAsync());
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_as_streaming_and_break_early_in_order_to_cancel_execution()
        {
            // Arrange
            var cli = Cli.Wrap("dotnet")
                .SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.LoopBoth)
                    .AddArgument(100_000_000));

            // Act
            var i = 0;
            await foreach (var _ in cli.StartEventStreamAsync())
            {
                if (i++ >= 100)
                    break;
            }

            // TODO: assert something?
        }
        */
    }
}