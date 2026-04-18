using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.Tests.Utils.Extensions;
using FluentAssertions;
using PowerKit;
using Xunit;

namespace CliWrap.Tests;

public class EnvironmentSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_a_custom_working_directory()
    {
        // Arrange
        using var dir = TempDirectory.Create();

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments("cwd")
            .WithWorkingDirectory(dir.Path);

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        // Resolve dir.Path to its canonical form to handle OS-level symlink resolution.
        // On some platforms (e.g., macOS), /var is a symlink to /private/var, so
        // Directory.GetCurrentDirectory() in the child process returns the resolved path.
        // We replicate this by temporarily setting the current directory, which triggers
        // the same OS path resolution (getcwd).
        var prevDir = Directory.GetCurrentDirectory();
        string resolvedDirPath;
        try
        {
            Directory.SetCurrentDirectory(dir.Path);
            resolvedDirPath = Directory.GetCurrentDirectory();
        }
        finally
        {
            Directory.SetCurrentDirectory(prevDir);
        }

        result.StandardOutput.Trim().Should().Be(resolvedDirPath);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_additional_environment_variables()
    {
        // Arrange
        var env = new Dictionary<string, string?> { ["foo"] = "bar", ["hello"] = "world" };

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["env", "foo", "hello"])
            .WithEnvironmentVariables(env);

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Should().ConsistOfLines("bar", "world");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_some_environment_variables_overwritten()
    {
        // Arrange
        var key = Guid.NewGuid();
        var variableToKeep = $"CLIWRAP_TEST_KEEP_{key}";
        var variableToOverwrite = $"CLIWRAP_TEST_OVERWRITE_{key}";
        var variableToUnset = $"CLIWRAP_TEST_UNSET_{key}";

        using (Environment.SetTempEnvironmentVariable(variableToKeep, "keep")) // will be left unchanged
        using (Environment.SetTempEnvironmentVariable(variableToOverwrite, "overwrite")) // will be overwritten
        using (Environment.SetTempEnvironmentVariable(variableToUnset, "unset")) // will be unset
        {
            var cmd = Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(["env", variableToKeep, variableToOverwrite, variableToUnset])
                .WithEnvironmentVariables(e =>
                    e.Set(variableToOverwrite, "overwritten").Set(variableToUnset, null)
                );

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.Should().ConsistOfLines("keep", "overwritten");
        }
    }
}
