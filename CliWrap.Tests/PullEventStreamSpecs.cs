using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.EventStream;
using FluentAssertions;
using PowerKit.Extensions;
using Xunit;

namespace CliWrap.Tests;

public class PullEventStreamSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_an_event_stream()
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
    public async Task I_can_execute_a_command_as_an_event_stream_and_abandon_it_early()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["generate text", "--target", "all", "--lines", "1000"]);

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
    public async Task I_can_execute_a_command_as_an_event_stream_and_not_hang_on_large_stdout_and_stderr()
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
    public async Task I_can_execute_a_command_as_an_event_stream_and_not_hang_on_large_stdout_and_stderr_if_I_abandon_it_early()
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
    public async Task I_can_execute_a_command_as_an_event_stream_and_cancel_it_immediately()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act
        var act = async () =>
        {
            await foreach (var cmdEvent in cmd.ListenAsync(cts.Token))
            {
                if (cmdEvent is StandardOutputCommandEvent stdOutEvent)
                    stdOutEvent.Text.Should().NotContain("Done.");
            }
        };

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.CancellationToken.Should()
            .Be(cts.Token);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_an_event_stream_and_cancel_it_after_a_delay()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(0.2));

        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act
        var act = async () =>
        {
            await foreach (var cmdEvent in cmd.ListenAsync(cts.Token))
            {
                if (cmdEvent is StandardOutputCommandEvent stdOutEvent)
                    stdOutEvent.Text.Should().NotContain("Done.");
            }
        };

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.CancellationToken.Should()
            .Be(cts.Token);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_an_event_stream_and_cancel_it_by_abandoning_it()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act
        var processId = -1;
        await foreach (var cmdEvent in cmd.ListenAsync())
        {
            if (cmdEvent is StartedCommandEvent startedEvent)
            {
                processId = startedEvent.ProcessId;
                break;
            }
            else if (cmdEvent is StandardOutputCommandEvent stdOutEvent)
            {
                stdOutEvent.Text.Should().NotContain("Done.");
            }
        }

        // Assert
        Process.IsRunning(processId).Should().BeFalse();

        // No exception in this scenario
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_an_event_stream_and_cancel_it_gracefully_after_a_delay()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(0.2));

        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act
        var act = async () =>
        {
            await foreach (
                var cmdEvent in cmd.ListenAsync(
                    Encoding.Default,
                    Encoding.Default,
                    CancellationToken.None,
                    cts.Token
                )
            )
            {
                if (cmdEvent is StandardOutputCommandEvent stdOutEvent)
                    stdOutEvent.Text.Should().NotContain("Done.");
            }
        };

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.CancellationToken.Should()
            .Be(cts.Token);
    }
}
