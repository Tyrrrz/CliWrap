using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.EventStream;
using FluentAssertions;
using PowerKit.Extensions;
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
        var act = async () => await task;

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.CancellationToken.Should()
            .Be(cts.Token);

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
        var act = async () => await task;

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.CancellationToken.Should()
            .Be(cts.Token);

        Process.IsRunning(task.ProcessId).Should().BeFalse();
        stdOutBuffer.ToString().Should().NotContain("Done.");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_cancel_it_gracefully_after_a_delay()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var stdOutBuffer = new StringBuilder();

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"])
            | PipeTarget.Merge(
                PipeTarget.ToDelegate(line =>
                {
                    // We need to send the cancellation request right after the process has registered
                    // a handler for the interrupt signal, otherwise the default handler will trigger
                    // and just kill the process.
                    if (line.Contains("Sleeping for", StringComparison.OrdinalIgnoreCase))
                        cts.CancelAfter(TimeSpan.FromSeconds(0.2));
                }),
                PipeTarget.ToStringBuilder(stdOutBuffer)
            );

        // Act
        var task = cmd.ExecuteAsync(CancellationToken.None, cts.Token);
        var act = async () => await task;

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.CancellationToken.Should()
            .Be(cts.Token);

        Process.IsRunning(task.ProcessId).Should().BeFalse();
        stdOutBuffer.ToString().Should().Contain("Canceled.").And.NotContain("Done.");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_buffering_and_cancel_it_immediately()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act
        var act = async () => await cmd.ExecuteBufferedAsync(cts.Token);

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.CancellationToken.Should()
            .Be(cts.Token);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_buffering_and_cancel_it_after_a_delay()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(0.2));

        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act
        var act = async () => await cmd.ExecuteBufferedAsync(cts.Token);

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.CancellationToken.Should()
            .Be(cts.Token);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_buffering_and_cancel_it_gracefully_after_a_delay()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(0.2));

        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act
        var act = async () =>
            await cmd.ExecuteBufferedAsync(
                Encoding.Default,
                Encoding.Default,
                CancellationToken.None,
                cts.Token
            );

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.CancellationToken.Should()
            .Be(cts.Token);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_pull_based_event_stream_and_cancel_it_immediately()
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
    public async Task I_can_execute_a_command_as_a_pull_based_event_stream_and_cancel_it_without_unobserved_exception()
    {
        // https://github.com/Tyrrrz/CliWrap/issues/336

        // Arrange
        var exception = default(Exception?);

        void OnUnobservedException(object? sender, UnobservedTaskExceptionEventArgs args)
        {
            Interlocked.CompareExchange(ref exception, args.Exception, null);
            args.SetObserved();
        }

        TaskScheduler.UnobservedTaskException += OnUnobservedException;

        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:20"]);

        // Act
        try
        {
            // This is not a deterministic test, so run it multiple times to increase exposure to the race condition
            for (var i = 0; i < 50; i++)
            {
                using var cts = new CancellationTokenSource();

                try
                {
                    await foreach (var _ in cmd.ListenAsync(cts.Token))
                    {
                        await cts.CancelAsync();
                    }
                }
                catch (OperationCanceledException) { }
            }

            await Task.Delay(500);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobservedException;
        }

        // Assert
        Volatile.Read(ref exception).Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_pull_based_event_stream_and_cancel_it_after_a_delay()
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
    public async Task I_can_execute_a_command_as_a_pull_based_event_stream_and_cancel_it_gracefully_after_a_delay()
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

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_push_based_event_stream_and_cancel_it_immediately()
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
    public async Task I_can_execute_a_command_as_a_push_based_event_stream_and_cancel_it_after_a_delay()
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
    public async Task I_can_execute_a_command_as_a_push_based_event_stream_and_cancel_it_gracefully_after_a_delay()
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
