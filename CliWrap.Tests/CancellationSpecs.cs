using System;
using System.Net.Http;
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

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.Location)
                    .Add(Dummy.Program.Sleep)
                    .Add(10_000));

            // Act & assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await cmd.ExecuteAsync(cts.Token));
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_cancel_execution_immediately()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.Location)
                    .Add(Dummy.Program.Sleep)
                    .Add(10_000));

            // Act & assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await cmd.ExecuteAsync(cts.Token));
        }

        public async Task Test()
        {
            var command = "Hello world" | Cli.Wrap("foo")
                .WithArguments("print random") | Cli.Wrap("bar")
                .WithArguments("reverse") | (Console.WriteLine, Console.Error.WriteLine);
        }
    }
}