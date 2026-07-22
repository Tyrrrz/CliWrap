using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CliWrap.EventStream;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class EventStreamSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_pull_based_event_stream()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["generate text", "--target", "all", "--lines", "100"]);

        // Act
        var events = new List<CommandEvent>();
        await foreach (var cmdEvent in cmd.ListenAsync())
            events.Add(cmdEvent);

        // Assert
        events.OfType<StartedCommandEvent>().Should().ContainSingle();
        events.OfType<StartedCommandEvent>().Single().ProcessId.Should().NotBe(0);
        events.OfType<StandardOutputCommandEvent>().Should().HaveCount(100);
        events.OfType<StandardErrorCommandEvent>().Should().HaveCount(100);
        events.OfType<ExitedCommandEvent>().Should().ContainSingle();
        events.OfType<ExitedCommandEvent>().Single().ExitCode.Should().Be(0);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_stop_listening_to_a_pull_based_event_stream_early()
    {
        // https://github.com/Tyrrrz/CliWrap/issues/336

        // Arrange
        var unobservedExceptions = new ConcurrentBag<Exception>();

        void OnUnobservedException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            unobservedExceptions.Add(e.Exception);
            e.SetObserved();
        }

        TaskScheduler.UnobservedTaskException += OnUnobservedException;

        // Keep the process alive after the iterator is disposed, so its completion continuation
        // attempts to close the channel after enumeration has stopped.
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments(["sleep", "00:00:00.1"]);

        // Act
        try
        {
            for (var i = 0; i < 10; i++)
            {
                await foreach (var _ in cmd.ListenAsync())
                {
                    break;
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Task.Delay(500);
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobservedException;
        }

        // Assert
        unobservedExceptions.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_push_based_event_stream()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["generate text", "--target", "all", "--lines", "100"]);

        // Act
        var events = await cmd.Observe().ToArray();

        // Assert
        events.OfType<StartedCommandEvent>().Should().ContainSingle();
        events.OfType<StartedCommandEvent>().Single().ProcessId.Should().NotBe(0);
        events.OfType<StandardOutputCommandEvent>().Should().HaveCount(100);
        events.OfType<StandardErrorCommandEvent>().Should().HaveCount(100);
        events.OfType<ExitedCommandEvent>().Should().ContainSingle();
        events.OfType<ExitedCommandEvent>().Single().ExitCode.Should().Be(0);
    }
}
