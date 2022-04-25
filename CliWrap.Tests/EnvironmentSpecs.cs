using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.Tests.Fixtures;
using CliWrap.Tests.Utils;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class EnvironmentSpecs : IClassFixture<TempOutputFixture>
{
    private readonly TempOutputFixture _tempOutput;

    public EnvironmentSpecs(TempOutputFixture tempOutputFixture) =>
        _tempOutput = tempOutputFixture;

    [Fact(Timeout = 15000)]
    public async Task Command_can_be_executed_with_a_custom_working_directory()
    {
        // Arrange
        var workingDirPath = _tempOutput.GetTempDirPath();

        var cmd = Cli.Wrap("dotnet")
            .WithWorkingDirectory(workingDirPath)
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("print cwd")
            );

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Trim().Should().Be(workingDirPath);
    }

    [Fact(Timeout = 15000)]
    public async Task Command_can_be_executed_with_additional_environment_variables()
    {
        // Arrange
        var env = new Dictionary<string, string?>
        {
            ["foo"] = "bar",
            ["hello"] = "world",
            ["Path"] = "there"
        };

        var stdOutLines = new List<string>();

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("print env")
            )
            .WithEnvironmentVariables(env) | stdOutLines.Add;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLines.Should().Contain(new[]
        {
            "[foo] = bar",
            "[hello] = world",
            "[Path] = there"
        });
    }

    [Fact(Timeout = 15000)]
    public async Task Command_can_be_executed_with_some_inherited_environment_variables_overwritten()
    {
        // Arrange
        var salt = Guid.NewGuid();
        var variableToKeep = $"CLIWRAP_TEST_KEEP_{salt}";
        var variableToOverwrite = $"CLIWRAP_TEST_OVERWRITE_{salt}";
        var variableToUnset = $"CLIWRAP_TEST_UNSET_{salt}";

        var stdOutLines = new List<string>();

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("print env")
            )
            .WithEnvironmentVariables(e => e
                .Set(variableToOverwrite, "new bar")
                .Set(variableToUnset, null)
            ) | stdOutLines.Add;

        // Act
        using (EnvironmentVariable.Set(variableToKeep, "foo")) // will be left unchanged
        using (EnvironmentVariable.Set(variableToOverwrite, "bar")) // will be overwritten
        using (EnvironmentVariable.Set(variableToUnset, "baz")) // will be unset
        {
            await cmd.ExecuteAsync();

            // Assert
            stdOutLines.Should().Contain(new[]
            {
                $"[{variableToKeep}] = foo",
                $"[{variableToOverwrite}] = new bar"
            });

            stdOutLines.Should().NotContain(new[]
            {
                $"[{variableToOverwrite}] = bar",
                $"[{variableToUnset}] = baz"
            });
        }
    }
}