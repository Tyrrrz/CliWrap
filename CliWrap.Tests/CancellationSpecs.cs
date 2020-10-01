using System;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.EventStream;
using CliWrap.Tests.Internal;
using CliWrap.Tests.Internal.Extensions;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class CancellationSpecs
    {
        [Fact(Timeout = 15000)]
        public async Task I_can_execute_a_command_and_cancel_it_immediately()
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
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();
        }

        [Fact(Timeout = 15000)]
        public async Task I_can_execute_a_command_and_cancel_it_while_it_is_in_progress()
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
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();
        }

        [Fact(Timeout = 15000)]
        public async Task I_can_execute_a_command_with_buffering_and_cancel_it_immediately()
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
            var task = cmd.ExecuteBufferedAsync(cts.Token);

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();
        }

        [Fact(Timeout = 15000)]
        public async Task I_can_execute_a_command_with_buffering_and_cancel_it_while_it_is_in_progress()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.Sleep)
                    .Add(10_000));

            // Act
            var task = cmd.ExecuteBufferedAsync(cts.Token);

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();
        }

        [Fact(Timeout = 15000)]
        public async Task I_can_execute_a_command_as_an_async_event_stream_and_cancel_it_immediately()
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
            var task = cmd.ListenAsync(cts.Token).IterateDiscardAsync();

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        }

        [Fact(Timeout = 15000)]
        public async Task I_can_execute_a_command_as_an_async_event_stream_and_cancel_it_while_it_is_in_progress()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.Sleep)
                    .Add(10_000));

            // Act
            var task = cmd.ListenAsync(cts.Token).IterateDiscardAsync();

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        }

        [Fact(Timeout = 15000)]
        public async Task I_can_execute_a_command_as_an_observable_event_stream_and_cancel_it_immediately()
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
            var task = cmd.Observe(cts.Token).ToTask(CancellationToken.None); // purposely don't provide token in ToTask()

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        }

        [Fact(Timeout = 15000)]
        public async Task I_can_execute_a_command_as_an_observable_event_stream_and_cancel_it_while_it_is_in_progress()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.Sleep)
                    .Add(10_000));

            // Act
            var task = cmd.Observe(cts.Token).ToTask(CancellationToken.None); // purposely don't provide token in ToTask()

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        }
    }
}