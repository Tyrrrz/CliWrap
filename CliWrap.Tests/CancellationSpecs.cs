using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.EventStream;
using CliWrap.Tests.Utils.Extensions;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class CancellationSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_cancel_it_immediately()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var stdOutBuffer = new StringBuilder();

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]) | stdOutBuffer;

        // Act
        var task = cmd.ExecuteAsync(cts.Token);

        // Assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        ex.CancellationToken.Should().Be(cts.Token);

        Process.IsRunning(task.ProcessId).Should().BeFalse();
        stdOutBuffer.ToString().Should().NotContain("Done.");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_cancel_it_after_a_delay()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(0.2));

        var stdOutBuffer = new StringBuilder();

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]) | stdOutBuffer;

        // Act
        var task = cmd.ExecuteAsync(cts.Token);

        // Assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        ex.CancellationToken.Should().Be(cts.Token);

        Process.IsRunning(task.ProcessId).Should().BeFalse();
        stdOutBuffer.ToString().Should().NotContain("Done.");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_cancel_it_gracefully_after_a_delay()
    {
        // Arrange
        using var forcefulCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        using var gracefulCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var stdOutBuffer = new StringBuilder();

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]) | stdOutBuffer;

        // Act
        var task = cmd.ExecuteAsync(forcefulCts.Token, gracefulCts.Token);

        // Assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        (ex.CancellationToken == gracefulCts.Token || ex.CancellationToken == forcefulCts.Token)
            .Should()
            .BeTrue();

        Process.IsRunning(task.ProcessId).Should().BeFalse();
        stdOutBuffer.ToString().Should().NotContain("Done.");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_buffering_and_cancel_it_immediately()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cmd.ExecuteBufferedAsync(cts.Token)
        );

        ex.CancellationToken.Should().Be(cts.Token);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_buffering_and_cancel_it_after_a_delay()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(0.2));

        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cmd.ExecuteBufferedAsync(cts.Token)
        );

        ex.CancellationToken.Should().Be(cts.Token);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_buffering_and_cancel_it_gracefully_after_a_delay()
    {
        // Arrange
        using var forcefulCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        using var gracefulCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cmd.ExecuteBufferedAsync(
                Encoding.Default,
                Encoding.Default,
                forcefulCts.Token,
                gracefulCts.Token
            )
        );

        (ex.CancellationToken == gracefulCts.Token || ex.CancellationToken == forcefulCts.Token)
            .Should()
            .BeTrue();
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_pull_based_event_stream_and_cancel_it_immediately()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var cmdEvent in cmd.ListenAsync(cts.Token))
            {
                if (cmdEvent is StandardOutputCommandEvent stdOutEvent)
                    stdOutEvent.Text.Should().NotContain("Done.");
            }
        });

        ex.CancellationToken.Should().Be(cts.Token);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_pull_based_event_stream_and_cancel_it_after_a_delay()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(0.2));

        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var cmdEvent in cmd.ListenAsync(cts.Token))
            {
                if (cmdEvent is StandardOutputCommandEvent stdOutEvent)
                    stdOutEvent.Text.Should().NotContain("Done.");
            }
        });

        ex.CancellationToken.Should().Be(cts.Token);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_pull_based_event_stream_and_cancel_it_gracefully_after_a_delay()
    {
        // Arrange
        using var forcefulCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        using var gracefulCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (
                var cmdEvent in cmd.ListenAsync(
                    Encoding.Default,
                    Encoding.Default,
                    forcefulCts.Token,
                    gracefulCts.Token
                )
            )
            {
                if (cmdEvent is StandardOutputCommandEvent stdOutEvent)
                    stdOutEvent.Text.Should().NotContain("Done.");
            }
        });

        (ex.CancellationToken == gracefulCts.Token || ex.CancellationToken == forcefulCts.Token)
            .Should()
            .BeTrue();
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_push_based_event_stream_and_cancel_it_immediately()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cmd.Observe(cts.Token)
                .ForEachAsync(
                    cmdEvent =>
                    {
                        if (cmdEvent is StandardOutputCommandEvent stdOutEvent)
                            stdOutEvent.Text.Should().NotContain("Done.");
                    },
                    CancellationToken.None
                )
        );

        ex.CancellationToken.Should().Be(cts.Token);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_push_based_event_stream_and_cancel_it_after_a_delay()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(0.2));

        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cmd.Observe(cts.Token)
                .ForEachAsync(
                    cmdEvent =>
                    {
                        if (cmdEvent is StandardOutputCommandEvent stdOutEvent)
                            stdOutEvent.Text.Should().NotContain("Done.");
                    },
                    CancellationToken.None
                )
        );

        ex.CancellationToken.Should().Be(cts.Token);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_push_based_event_stream_and_cancel_it_gracefully_after_a_delay()
    {
        // Arrange
        using var forcefulCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        using var gracefulCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cmd.Observe(
                    Encoding.Default,
                    Encoding.Default,
                    forcefulCts.Token,
                    gracefulCts.Token
                )
                .ForEachAsync(
                    cmdEvent =>
                    {
                        if (cmdEvent is StandardOutputCommandEvent stdOutEvent)
                            stdOutEvent.Text.Should().NotContain("Done.");
                    },
                    CancellationToken.None
                )
        );

        (ex.CancellationToken == gracefulCts.Token || ex.CancellationToken == forcefulCts.Token)
            .Should()
            .BeTrue();
    }
}
