using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.EventStream;
using CliWrap.Tests.Utils;
using CliWrap.Tests.Utils.Extensions;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class CancellationSpecs
{
    [Fact(Timeout = 15000)]
    public async Task Command_execution_can_be_canceled_immediately()
    {
        // Arrange
        var stdOutLines = new List<string>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("sleep")
                .Add("--duration").Add("00:00:20")
            ) | stdOutLines.Add;

        // Act
        var task = cmd.ExecuteAsync(cts.Token);

        // Assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();
        ex.CancellationToken.Should().Be(cts.Token);

        stdOutLines.Should().NotContainEquivalentOf("Done.");
    }

    [Fact(Timeout = 15000)]
    public async Task Command_execution_can_be_canceled_while_it_is_in_progress()
    {
        // Arrange
        var stdOutLines = new List<string>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("sleep")
                .Add("--duration").Add("00:00:20")
            ) | stdOutLines.Add;

        // Act
        var task = cmd.ExecuteAsync(cts.Token);

        // Assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();
        ex.CancellationToken.Should().Be(cts.Token);

        stdOutLines.Should().NotContainEquivalentOf("Done.");
    }

    [Fact(Timeout = 15000)]
    public async Task Command_execution_can_be_canceled_gracefully()
    {
        // Arrange
        var stdOutLines = new List<string>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("sleep")
                .Add("--duration").Add("00:00:20")
            ) | stdOutLines.Add;

        // Act
        var task = cmd.ExecuteAsync(
            new CommandCancellation(
                cts.Token,
                CancellationToken.None
            )
        );

        // Assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();
        ex.CancellationToken.Should().Be(cts.Token);

        stdOutLines.Should().ContainEquivalentOf("Operation cancelled gracefully.");
        stdOutLines.Should().NotContainEquivalentOf("Done.");
    }

    [Fact(Timeout = 15000)]
    public async Task Buffered_command_execution_can_be_canceled_immediately()
    {
        // Arrange
        var stdOutLines = new List<string>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("sleep")
                .Add("--duration").Add("00:00:20")
            ) | stdOutLines.Add;

        // Act
        var task = cmd.ExecuteBufferedAsync(cts.Token);

        // Assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();
        ex.CancellationToken.Should().Be(cts.Token);

        stdOutLines.Should().NotContainEquivalentOf("Done.");
    }

    [Fact(Timeout = 15000)]
    public async Task Buffered_command_execution_can_be_canceled_while_it_is_in_progress()
    {
        // Arrange
        var stdOutLines = new List<string>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("sleep")
                .Add("--duration").Add("00:00:20")
            ) | stdOutLines.Add;

        // Act
        var task = cmd.ExecuteBufferedAsync(cts.Token);

        // Assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();
        ex.CancellationToken.Should().Be(cts.Token);

        stdOutLines.Should().NotContainEquivalentOf("Done.");
    }

    [Fact(Timeout = 15000)]
    public async Task Pull_event_stream_command_execution_can_be_canceled_immediately()
    {
        // Arrange
        var stdOutLines = new List<string>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("sleep")
                .Add("--duration").Add("00:00:20")
            );

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var cmdEvent in cmd.ListenAsync(cts.Token))
            {
                if (cmdEvent is StandardErrorCommandEvent stdOutCmdEvent)
                    stdOutLines.Add(stdOutCmdEvent.Text);
            }
        });

        ex.CancellationToken.Should().Be(cts.Token);

        stdOutLines.Should().NotContainEquivalentOf("Done.");
    }

    [Fact(Timeout = 15000)]
    public async Task Pull_event_stream_command_execution_can_be_canceled_while_it_is_in_progress()
    {
        // Arrange
        var stdOutLines = new List<string>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("sleep")
                .Add("--duration").Add("00:00:20")
            );

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var cmdEvent in cmd.ListenAsync(cts.Token))
            {
                if (cmdEvent is StandardErrorCommandEvent stdOutCmdEvent)
                    stdOutLines.Add(stdOutCmdEvent.Text);
            }
        });

        ex.CancellationToken.Should().Be(cts.Token);

        stdOutLines.Should().NotContainEquivalentOf("Done.");
    }

    [Fact(Timeout = 15000)]
    public async Task Push_event_stream_command_execution_can_be_canceled_immediately()
    {
        // Arrange
        var stdOutLines = new List<string>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("sleep")
                .Add("--duration").Add("00:00:20")
            );

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await cmd.Observe(cts.Token).ForEachAsync(cmdEvent =>
            {
                if (cmdEvent is StandardErrorCommandEvent stdOutCmdEvent)
                    stdOutLines.Add(stdOutCmdEvent.Text);
            }, CancellationToken.None);
        });

        ex.CancellationToken.Should().Be(cts.Token);

        stdOutLines.Should().NotContainEquivalentOf("Done.");
    }

    [Fact(Timeout = 15000)]
    public async Task Push_event_stream_command_execution_can_be_canceled_while_it_is_in_progress()
    {
        // Arrange
        var stdOutLines = new List<string>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("sleep")
                .Add("--duration").Add("00:00:20")
            );

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await cmd.Observe(cts.Token).ForEachAsync(cmdEvent =>
            {
                if (cmdEvent is StandardErrorCommandEvent stdOutCmdEvent)
                    stdOutLines.Add(stdOutCmdEvent.Text);
            }, CancellationToken.None);
        });

        ex.CancellationToken.Should().Be(cts.Token);

        stdOutLines.Should().NotContainEquivalentOf("Done.");
    }
}