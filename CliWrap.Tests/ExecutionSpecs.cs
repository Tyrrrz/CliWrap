using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using PowerKit.Extensions;
using Xunit;

namespace CliWrap.Tests;

public class ExecutionSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_get_its_exit_code_and_execution_time()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath);

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(0);
        result.IsSuccess.Should().BeTrue();
        result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_use_an_implicit_conversion_to_get_its_exit_code()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath);

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        ((int)result)
            .Should()
            .Be(0);
        ((bool)result).Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_get_its_associated_process_ID()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath);

        // Act
        var task = cmd.ExecuteAsync();

        // Assert
        task.ProcessId.Should().NotBe(0);

        await task;
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_a_configured_awaiter()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath);

        // Act & assert
        await cmd.ExecuteAsync().ConfigureAwait(false);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_manually_configured_process_settings()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithValidation(CommandResultValidation.None);

        // Act
        var result = await cmd.ExecuteAsync(
            startInfo => startInfo.Arguments = "exit 13",
            process => process.PriorityBoostEnabled = false
        );

        // Assert
        result.ExitCode.Should().Be(13);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_not_hang_on_large_stdout_and_stderr()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["generate binary", "--target", "all", "--length", "100000"]);

        // Act & assert
        await cmd.ExecuteAsync();
    }

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

    [Fact]
    public void I_can_try_to_execute_a_command_and_get_an_error_if_the_target_file_does_not_exist()
    {
        // Arrange
        var cmd = Cli.Wrap("I_do_not_exist.exe");

        // Act
        var act = () => cmd.ExecuteAsync();

        // Assert
        // Should throw synchronously
        // https://github.com/Tyrrrz/CliWrap/issues/139
        act.Should().Throw<Win32Exception>();
    }
}
