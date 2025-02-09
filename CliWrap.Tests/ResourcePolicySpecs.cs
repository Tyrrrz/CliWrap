using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class ResourcePolicySpecs
{
    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_custom_process_priority()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithResourcePolicy(p => p.SetPriority(ProcessPriorityClass.High));

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(0);
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_custom_affinity_mask()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithResourcePolicy(p => p.SetAffinity(0b1010)); // Cores 1 and 3

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(0);
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_custom_working_set_limits()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithResourcePolicy(p =>
                p.SetMinWorkingSet(1024 * 1024) // 1 MB
                    .SetMaxWorkingSet(1024 * 1024 * 10) // 10 MB
            );

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(0);
    }
}
