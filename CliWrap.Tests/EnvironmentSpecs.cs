using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.Tests.Utils;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class EnvironmentSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_a_custom_working_directory()
    {
        // Arrange
        using var dir = TempDir.Create();

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("print cwd")
            )
            .WithWorkingDirectory(dir.Path);

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Trim().Should().Be(dir.Path);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_additional_environment_variables()
    {
        // Arrange
        var env = new Dictionary<string, string?>
        {
            ["foo"] = "bar",
            ["hello"] = "world",
            ["Path"] = "there"
        };

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("print env")
            )
            .WithEnvironmentVariables(env);

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Trim().Should().ContainAll(
            "[foo] = bar",
            "[hello] = world",
            "[Path] = there"
        );
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_some_environment_variables_overwritten()
    {
        // Arrange
        var key = Guid.NewGuid();
        var variableToKeep = $"CLIWRAP_TEST_KEEP_{key}";
        var variableToOverwrite = $"CLIWRAP_TEST_OVERWRITE_{key}";
        var variableToUnset = $"CLIWRAP_TEST_UNSET_{key}";

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("print env")
            )
            .WithEnvironmentVariables(e => e
                .Set(variableToOverwrite, "overwritten")
                .Set(variableToUnset, null)
            );

        // Act
        using (EnvironmentVariable.Set(variableToKeep, "keep")) // will be left unchanged
        using (EnvironmentVariable.Set(variableToOverwrite, "overwrite")) // will be overwritten
        using (EnvironmentVariable.Set(variableToUnset, "unset")) // will be unset
        {
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.Trim().Should().ContainAll(
                $"[{variableToKeep}] = keep",
                $"[{variableToOverwrite}] = overwritten"
            );

            result.StandardOutput.Trim().Should().NotContainAny(
                $"[{variableToOverwrite}] = overwrite",
                $"[{variableToUnset}] = unset"
            );
        }
    }
}