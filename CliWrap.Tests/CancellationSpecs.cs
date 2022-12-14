using System;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.EventStream;
using CliWrap.Tests.Utils;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class CancellationSpecs
{
    [Fact(Timeout = 15000)]
    public async Task Command_execution_can_be_canceled_immediately()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var stdOutLines = new List<string>();

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
        ex.CancellationToken.Should().Be(cts.Token);

        ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();

        stdOutLines.Should().NotContainEquivalentOf("Done.");
    }

    [Fact(Timeout = 15000)]
    public async Task Command_execution_can_be_canceled_while_it_is_in_progress()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(0.5));

        var stdOutLines = new List<string>();

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
        ex.CancellationToken.Should().Be(cts.Token);

        ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();

        stdOutLines.Should().NotContainEquivalentOf("Done.");
    }

    [SkippableFact(Timeout = 15000)]
    public async Task Command_execution_can_be_canceled_gracefully_while_it_is_in_progress()
    {
        Skip.If(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "Graceful cancellation is only supported on Windows."
        );

        // Arrange
        using var cts = new CommandCancellationTokenSource();

        // We need to send the cancellation request right after the process has registered
        // a handler for the interrupt signal, otherwise the default handler will trigger
        // and just kill the process.
        void HandleStdOut(string line)
        {
            if (line.Contains("Sleeping for", StringComparison.OrdinalIgnoreCase))
                cts.CancelGracefullyAfter(TimeSpan.FromSeconds(0.5));
        }

        var stdOutLines = new List<string>();

        var pipeTarget = PipeTarget.Merge(
            PipeTarget.ToDelegate(HandleStdOut),
            PipeTarget.ToDelegate(stdOutLines.Add)
        );

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("sleep")
                .Add("--duration").Add("00:00:20")
            ) | pipeTarget;

        // Act
        var task = cmd.ExecuteAsync(cts.Token);

        // Assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        ex.CancellationToken.Should().Be(cts.Token.Graceful);

        ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();

        stdOutLines.Should().ContainEquivalentOf("Canceled.");
        stdOutLines.Should().NotContainEquivalentOf("Done.");
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
                .Add("--duration").Add("00:00:20")
            );

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cmd.ExecuteBufferedAsync(cts.Token)
        );

        ex.CancellationToken.Should().Be(cts.Token);
    }

    [Fact(Timeout = 15000)]
    public async Task Buffered_command_execution_can_be_canceled_while_it_is_in_progress()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(0.5));

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("sleep")
                .Add("--duration").Add("00:00:20")
            );

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cmd.ExecuteBufferedAsync(cts.Token)
        );

        ex.CancellationToken.Should().Be(cts.Token);
    }

    [SkippableFact(Timeout = 15000)]
    public async Task Buffered_command_execution_can_be_canceled_gracefully_while_it_is_in_progress()
    {
        Skip.If(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "Graceful cancellation is only supported on Unix."
        );

        // Arrange
        using var cts = new CommandCancellationTokenSource();
        cts.CancelGracefullyAfter(TimeSpan.FromSeconds(0.5));

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("sleep")
                .Add("--duration").Add("00:00:20")
            );

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cmd.ExecuteBufferedAsync(Console.OutputEncoding, Console.OutputEncoding, cts.Token)
        );

        ex.CancellationToken.Should().Be(cts.Token.Graceful);
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
                .Add("--duration").Add("00:00:20")
            );

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in cmd.ListenAsync(cts.Token))
            {
            }
        });

        ex.CancellationToken.Should().Be(cts.Token);
    }

    [Fact(Timeout = 15000)]
    public async Task Pull_event_stream_command_execution_can_be_canceled_while_it_is_in_progress()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(0.5));

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("sleep")
                .Add("--duration").Add("00:00:20")
            );

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in cmd.ListenAsync(cts.Token))
            {
            }
        });

        ex.CancellationToken.Should().Be(cts.Token);
    }

    [SkippableFact(Timeout = 15000)]
    public async Task Pull_event_stream_command_execution_can_be_canceled_gracefully_while_it_is_in_progress()
    {
        Skip.If(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "Graceful cancellation is only supported on Unix."
        );

        // Arrange
        using var cts = new CommandCancellationTokenSource();
        cts.CancelGracefullyAfter(TimeSpan.FromSeconds(0.5));

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("sleep")
                .Add("--duration").Add("00:00:20")
            );

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in cmd.ListenAsync(Console.OutputEncoding, Console.OutputEncoding, cts.Token))
            {
            }
        });

        ex.CancellationToken.Should().Be(cts.Token.Graceful);
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
                .Add("--duration").Add("00:00:20")
            );

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cmd.Observe(cts.Token).ToTask(CancellationToken.None)
        );

        ex.CancellationToken.Should().Be(cts.Token);
    }

    [Fact(Timeout = 15000)]
    public async Task Push_event_stream_command_execution_can_be_canceled_while_it_is_in_progress()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(0.5));

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("sleep")
                .Add("--duration").Add("00:00:20")
            );

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cmd.Observe(cts.Token).ToTask(CancellationToken.None)
        );

        ex.CancellationToken.Should().Be(cts.Token);
    }

    [SkippableFact(Timeout = 15000)]
    public async Task Push_event_stream_command_execution_can_be_canceled_gracefully_while_it_is_in_progress()
    {
        Skip.If(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "Graceful cancellation is only supported on Unix."
        );

        // Arrange
        using var cts = new CommandCancellationTokenSource();
        cts.CancelGracefullyAfter(TimeSpan.FromSeconds(0.5));

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("sleep")
                .Add("--duration").Add("00:00:20")
            );

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cmd.Observe(Console.OutputEncoding, Console.OutputEncoding, cts.Token).ToTask(CancellationToken.None)
        );

        ex.CancellationToken.Should().Be(cts.Token.Graceful);
    }
}