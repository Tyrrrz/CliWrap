using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CliWrap.EventStream;
using FluentAssertions;
using PowerKit.Extensions;
using Xunit;

namespace CliWrap.Tests;

public class EventStreamSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_pull_based_event_stream()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["generate text", "--target", "all", "--lines", "1000"]);

        // Act
        var events = new List<CommandEvent>();
        await foreach (var cmdEvent in cmd.ListenAsync())
            events.Add(cmdEvent);

        // Assert
        events.OfType<StartedCommandEvent>().Should().ContainSingle();
        events.OfType<StartedCommandEvent>().Single().ProcessId.Should().NotBe(0);
        events.OfType<StandardOutputCommandEvent>().Should().HaveCount(1000);
        events.OfType<StandardErrorCommandEvent>().Should().HaveCount(1000);
        events.OfType<ExitedCommandEvent>().Should().ContainSingle();
        events.OfType<ExitedCommandEvent>().Single().ExitCode.Should().Be(0);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_pull_based_event_stream_and_break_out_early()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act: start listening but break out immediately after the first event
        var processId = 0;
        await foreach (var cmdEvent in cmd.ListenAsync())
        {
            if (cmdEvent is StartedCommandEvent startedEvent)
                processId = startedEvent.ProcessId;

            break;
        }

        // Assert: the process is not left running in the background
        Process.IsRunning(processId).Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_pull_based_event_stream_and_not_hang_on_large_stdout_and_stderr()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments([
                "generate text",
                "--target",
                "all",
                "--length",
                "10000000",
                "--lines",
                "1000",
            ]);

        // Act & assert
        await foreach (var _ in cmd.ListenAsync())
        {
            // Drain the stream, see if the test times out
        }
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_pull_based_event_stream_and_not_hang_on_large_stdout_and_stderr_if_I_break_out_early()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments([
                "generate text",
                "--target",
                "all",
                "--length",
                "10000000",
                "--lines",
                "1000",
            ]);

        // Act
        var i = 0;
        await foreach (var _ in cmd.ListenAsync())
        {
            if (++i >= 10)
                break;
        }

        // Assert
        i.Should().Be(10);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_push_based_event_stream()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["generate text", "--target", "all", "--lines", "1000"]);

        // Act
        var events = await cmd.Observe().ToArray();

        // Assert
        events.OfType<StartedCommandEvent>().Should().ContainSingle();
        events.OfType<StartedCommandEvent>().Single().ProcessId.Should().NotBe(0);
        events.OfType<StandardOutputCommandEvent>().Should().HaveCount(1000);
        events.OfType<StandardErrorCommandEvent>().Should().HaveCount(1000);
        events.OfType<ExitedCommandEvent>().Should().ContainSingle();
        events.OfType<ExitedCommandEvent>().Single().ExitCode.Should().Be(0);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_push_based_event_stream_and_not_hang_on_large_stdout_and_stderr()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments([
                "generate text",
                "--target",
                "all",
                "--length",
                "10000000",
                "--lines",
                "1000",
            ]);

        // Act & assert
        await cmd.Observe().ToArray();
    }
}
