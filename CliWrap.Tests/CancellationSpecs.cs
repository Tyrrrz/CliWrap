using System;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Tests.Internal;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class CancellationSpecs
    {
        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_cancel_execution_immediately()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.Sleep)
                    .Add(10_000));

            // Act
            var task = cmd.ExecuteAsync(cts.Token);

            // Assert
            await Assert.ThrowsAsync<TaskCanceledException>(() => task);
            ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_and_cancel_execution_while_it_is_in_progress()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.Sleep)
                    .Add(10_000));

            // Act
            var task = cmd.ExecuteAsync(cts.Token);

            // Assert
            await Assert.ThrowsAsync<TaskCanceledException>(() => task);
            ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();
        }
    }
}