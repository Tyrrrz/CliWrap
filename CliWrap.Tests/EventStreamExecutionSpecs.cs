using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.EventStream;
using CliWrap.Tests.Utils.Extensions;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class EventStreamExecutionSpecs
    {
        [Fact(Timeout = 15000)]
        public async Task Command_can_be_executed_as_a_pull_event_stream()
        {
            // Arrange
            const int expectedLinesCount = 100;

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("generate-text-lines")
                    .Add("--target").Add("all")
                    .Add("--count").Add(expectedLinesCount));

            // Act
            var stdOutLinesCount = 0;
            var stdErrLinesCount = 0;
            var processHasExited = false;

            await foreach (var cmdEvent in cmd.ListenAsync())
            {
                switch (cmdEvent)
                {
                    case StartedCommandEvent started:
                        started.ProcessId.Should().NotBe(0);
                        break;
                    case StandardOutputCommandEvent stdOut:
                        stdOut.Text.Should().NotBeNullOrEmpty();
                        stdOutLinesCount++;
                        break;
                    case StandardErrorCommandEvent stdErr:
                        stdErr.Text.Should().NotBeNullOrEmpty();
                        stdErrLinesCount++;
                        break;
                    case ExitedCommandEvent exited:
                        exited.ExitCode.Should().Be(0);
                        processHasExited = true;
                        break;
                }
            }

            // Assert
            stdOutLinesCount.Should().Be(expectedLinesCount);
            stdErrLinesCount.Should().Be(expectedLinesCount);
            processHasExited.Should().BeTrue();
        }

        [Fact(Timeout = 15000)]
        public async Task Command_can_be_executed_as_a_push_event_stream()
        {
            // Arrange
            const int expectedLinesCount = 100;

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("generate-text-lines")
                    .Add("--target").Add("all")
                    .Add("--count").Add(expectedLinesCount));

            // Act
            var stdOutLinesCount = 0;
            var stdErrLinesCount = 0;
            var processHasExited = false;

            await cmd.Observe().ForEachAsync(cmdEvent =>
            {
                switch (cmdEvent)
                {
                    case StartedCommandEvent started:
                        started.ProcessId.Should().NotBe(0);
                        break;
                    case StandardOutputCommandEvent stdOut:
                        stdOut.Text.Should().NotBeNullOrEmpty();
                        stdOutLinesCount++;
                        break;
                    case StandardErrorCommandEvent stdErr:
                        stdErr.Text.Should().NotBeNullOrEmpty();
                        stdErrLinesCount++;
                        break;
                    case ExitedCommandEvent exited:
                        exited.ExitCode.Should().Be(0);
                        processHasExited = true;
                        break;
                }
            });

            // Assert
            stdOutLinesCount.Should().Be(expectedLinesCount);
            stdErrLinesCount.Should().Be(expectedLinesCount);
            processHasExited.Should().BeTrue();
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