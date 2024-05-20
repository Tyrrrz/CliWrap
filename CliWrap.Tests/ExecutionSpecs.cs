using System;
using System.ComponentModel;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class ExecutionSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_get_the_exit_code_and_execution_time()
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
    public async Task I_can_execute_a_command_and_get_the_associated_process_ID()
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
    public async Task I_can_execute_a_command_and_not_hang_on_large_stdout_and_stderr()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["generate binary", "--target", "all", "--length", "100000"]);

        // Act & assert
        await cmd.ExecuteAsync();
    }

    [Fact]
    public void I_can_try_to_execute_a_command_and_get_an_error_if_the_target_file_does_not_exist()
    {
        // Arrange
        var cmd = Cli.Wrap("I_do_not_exist.exe");

        // Act & assert

        // Should throw synchronously
        // https://github.com/Tyrrrz/CliWrap/issues/139
        Assert.ThrowsAny<Win32Exception>(
            () =>
                // xUnit tells us to use ThrowsAnyAsync(...) instead for async methods,
                // but we're actually interested in the sync portion of this method.
                // So cast the result to object to avoid the warning.
                (object)cmd.ExecuteAsync()
        );
    }
}
