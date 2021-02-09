using System;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.EventStream;
using CliWrap.Tests.Utils;
using CliWrap.Tests.Utils.Extensions;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class CancellationSpecs
    {
        [Fact(Timeout = 15000)]
        public async Task Command_execution_can_be_canceled_immediately()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("sleep")
                    .Add("--duration").Add("00:00:10"));

            // Act
            var task = cmd.ExecuteAsync(cts.Token);

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();
        }

        [Fact(Timeout = 15000)]
        public async Task Command_execution_can_be_canceled_while_it_is_in_progress()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("sleep")
                    .Add("--duration").Add("00:00:10"));

            // Act
            var task = cmd.ExecuteAsync(cts.Token);

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();
        }

        [Fact(Timeout = 15000)]
        public async Task Buffered_command_execution_can_be_canceled_immediately()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("sleep")
                    .Add("--duration").Add("00:00:10"));

            // Act
            var task = cmd.ExecuteBufferedAsync(cts.Token);

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();
        }

        [Fact(Timeout = 15000)]
        public async Task Buffered_command_execution_can_be_canceled_while_it_is_in_progress()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("sleep")
                    .Add("--duration").Add("00:00:10"));

            // Act
            var task = cmd.ExecuteBufferedAsync(cts.Token);

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();
        }

        [Fact(Timeout = 15000)]
        public async Task Pull_event_stream_command_execution_can_be_canceled_immediately()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("sleep")
                    .Add("--duration").Add("00:00:10"));

            // Act & assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await cmd.ListenAsync(cts.Token).IterateDiscardAsync()
            );
        }

        [Fact(Timeout = 15000)]
        public async Task Pull_event_stream_command_execution_can_be_canceled_while_it_is_in_progress()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("sleep")
                    .Add("--duration").Add("00:00:10"));

            // Act & assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await cmd.ListenAsync(cts.Token).IterateDiscardAsync()
            );
        }

        [Fact(Timeout = 15000)]
        public async Task Push_event_stream_command_execution_can_be_canceled_immediately()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("sleep")
                    .Add("--duration").Add("00:00:10"));

            // Act & assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await cmd.Observe(cts.Token).ToTask(CancellationToken.None)
            );
        }

        [Fact(Timeout = 15000)]
        public async Task Push_event_stream_command_execution_can_be_canceled_while_it_is_in_progress()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("sleep")
                    .Add("--duration").Add("00:00:10"));

            // Act & assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await cmd.Observe(cts.Token).ToTask(CancellationToken.None)
            );
        }
    }
}