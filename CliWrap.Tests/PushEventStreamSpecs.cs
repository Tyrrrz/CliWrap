using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.EventStream;
using FluentAssertions;
using PowerKit.Extensions;
using Xunit;

namespace CliWrap.Tests;

public class PushEventStreamSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_an_event_stream()
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
    public async Task I_can_execute_a_command_as_an_event_stream_and_unsubscribe_from_it_early()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["generate text", "--target", "all", "--lines", "1000"]);

        // Act
        var events = await cmd.Observe().Take(10).ToArray();

        // Assert
        events.Should().HaveCount(10);
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
        await cmd.Observe().ToArray();
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_an_event_stream_and_not_hang_on_large_stdout_and_stderr_if_I_unsubscribe_from_it_early()
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
        var events = await cmd.Observe().Take(10).ToArray();

        // Assert
        events.Should().HaveCount(10);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_an_event_stream_and_cancel_it_by_unsubscribing_from_it()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act
        var startedEvent = await cmd.Observe().OfType<StartedCommandEvent>().FirstAsync();

        // Assert
        try
        {
            // The observable returns synchronously but the process gets terminated asynchronously
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var process = Process.GetProcessById(startedEvent.ProcessId);
            await process.WaitForExitAsync(cts.Token);
        }
        catch { }

        Process.IsRunning(startedEvent.ProcessId).Should().BeFalse();
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
            await cmd.Observe(cts.Token)
                .ForEachAsync(
                    cmdEvent =>
                    {
                        if (cmdEvent is StandardOutputCommandEvent stdOutEvent)
                            stdOutEvent.Text.Should().NotContain("Done.");
                    },
                    CancellationToken.None
                );

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
            await cmd.Observe(cts.Token)
                .ForEachAsync(
                    cmdEvent =>
                    {
                        if (cmdEvent is StandardOutputCommandEvent stdOutEvent)
                            stdOutEvent.Text.Should().NotContain("Done.");
                    },
                    CancellationToken.None
                );

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.CancellationToken.Should()
            .Be(cts.Token);
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
            await cmd.Observe(Encoding.Default, Encoding.Default, CancellationToken.None, cts.Token)
                .ForEachAsync(
                    cmdEvent =>
                    {
                        if (cmdEvent is StandardOutputCommandEvent stdOutEvent)
                            stdOutEvent.Text.Should().NotContain("Done.");
                    },
                    CancellationToken.None
                );

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.CancellationToken.Should()
            .Be(cts.Token);
    }
}
