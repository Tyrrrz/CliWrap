﻿using System;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Tests.Utils;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class ExecutionSpecs
{
    [Fact(Timeout = 15000)]
    public async Task Command_can_be_executed_which_yields_a_result_containing_runtime_information()
    {
        // Arrange
        var cmd = Cli.Wrap("dotnet").WithArguments(Dummy.Program.FilePath);

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(0);
        result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact(Timeout = 15000)]
    public async Task Underlying_process_ID_can_be_obtained_while_a_command_is_executing()
    {
        // Arrange
        var cmd = Cli.Wrap("dotnet").WithArguments(Dummy.Program.FilePath);

        // Act
        var task = cmd.ExecuteAsync();

        // Assert
        task.ProcessId.Should().NotBe(0);

        await task;
    }

    [Fact(Timeout = 15000)]
    public async Task Command_can_be_executed_with_a_configured_awaiter()
    {
        // Arrange
        var cmd = Cli.Wrap("dotnet").WithArguments(Dummy.Program.FilePath);

        // Act
        var result = await cmd.ExecuteAsync().ConfigureAwait(false);

        // Assert
        result.ExitCode.Should().Be(0);
    }

    [Fact(Timeout = 15000)]
    public async Task Command_execution_can_be_canceled_immediately()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("sleep")
                .Add("--duration").Add("00:00:10"));

        // Act
        var task = cmd.ExecuteAsync(cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public async Task Command_execution_can_be_canceled_while_it_is_in_progress()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("sleep")
                .Add("--duration").Add("00:00:10"));

        // Act
        var task = cmd.ExecuteAsync(cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        ProcessEx.IsRunning(task.ProcessId).Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public async Task Command_execution_does_not_deadlock_on_large_stdout_and_stderr()
    {
        // Arrange
        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("generate-text")
                .Add("--target").Add("all")
                .Add("--length").Add(100_000));

        // Act
        await cmd.ExecuteAsync();
    }

    [Fact(Timeout = 15000)]
    public async Task Command_execution_with_file_not_exist_fire_InvalidOperationException()
    {
        // Arrange        
        var cmd = Cli.Wrap("app_not_exist.exe");

        // Act         
        var act = async () => await cmd.ExecuteAsync();
        //Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
           .Where(e => e.Message.Contains("This could mean that the target executable doesn't exist or that execute permission is missing"));

    }
}