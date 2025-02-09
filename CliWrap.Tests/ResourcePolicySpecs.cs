using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class ResourcePolicySpecs
{
    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_a_custom_process_priority()
    {
        // Process priority is supported on other platforms, but setting it requires elevated permissions,
        // which we cannot guarantee in a CI environment. Therefore, we only test this on Windows.
        Skip.IfNot(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "Starting a process with a custom priority is only supported on Windows."
        );

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithResourcePolicy(p => p.SetPriority(ProcessPriorityClass.High));

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(0);
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_a_custom_core_affinity()
    {
        Skip.IfNot(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                || RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
            "Starting a process with a custom core affinity is only supported on Windows and Linux."
        );

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithResourcePolicy(p => p.SetAffinity(0b1010)); // Cores 1 and 3

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(0);
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_a_custom_working_set_limit()
    {
        Skip.IfNot(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "Starting a process with a custom working set limit is only supported on Windows."
        );

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

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_try_to_execute_a_command_with_a_custom_resource_policy_and_get_an_error_if_the_operating_system_does_not_support_it()
    {
        Skip.If(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "Starting a process with a custom resource policy is fully supported on Windows."
        );

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithResourcePolicy(p => p.SetMinWorkingSet(1024 * 1024));

        // Act & assert
        await Assert.ThrowsAsync<NotSupportedException>(() => cmd.ExecuteAsync());
    }
}
