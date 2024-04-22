using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class ExitConditionSpecs()
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_that_creates_child_process_resuing_standard_output_and_finish_after_child_process_exits()
    {
        // Arrange
        var cmd = this.PrepareCommand(line => { });

        // Act
        var executionStart = DateTime.UtcNow;
        var result = await cmd.ExecuteAsync();
        var executionFinish = DateTime.UtcNow;

        // Assert
        executionFinish
            .Subtract(executionStart)
            .Should()
            .BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(3));
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_that_creates_child_process_resuing_standard_output_and_finish_instantly_after_main_process_exits()
    {
        // Arrange
        int childProcessId = -1;
        var cmd = this.PrepareCommand(line => childProcessId = Convert.ToInt32(line.Trim()))
            .WithExitCondition(CommandExitCondition.ProcessExited);

        // Act
        var executionStart = DateTime.UtcNow;
        var result = await cmd.ExecuteAsync();
        var executionFinish = DateTime.UtcNow;

        var process = Process.GetProcessById(childProcessId);

        // Assert
        executionFinish.Subtract(executionStart).Should().BeLessThan(TimeSpan.FromSeconds(3));

        process.HasExited.Should().BeFalse();
    }

    /// <summary>
    /// Prepares a command that will create a sleeping child process and return its id via standard output.
    /// </summary>
    private Command PrepareCommand(Action<string> onStandardOutput)
    {
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(
                [
                    "run",
                    "process",
                    "--path",
                    Dummy.Program.FilePath,
                    "--arguments",
                    "sleep 00:00:03"
                ]
            )
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line => onStandardOutput(line)))
            .WithStandardErrorPipe(
                PipeTarget.ToDelegate(line => Console.WriteLine($"Error: {line}"))
            );

        return cmd;
    }
}
