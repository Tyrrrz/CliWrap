using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CliWrap.Buffered;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class EnvironmentSpecs
    {
        [Fact(Timeout = 15000)]
        public async Task Command_can_be_executed_with_a_custom_working_directory()
        {
            // Arrange
            var workingDirPath = Directory.GetParent(Directory.GetCurrentDirectory())!.FullName;

            var cmd = Cli.Wrap("dotnet")
                .WithWorkingDirectory(workingDirPath)
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("print-working-dir"));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.Should().Be(workingDirPath);
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

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add("print-environment-variables"))
                .WithEnvironmentVariables(env);

            // Act
            var result = await cmd.ExecuteBufferedAsync();
            var stdOutLines = result.StandardOutput.Split(Environment.NewLine);

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
            var variableToKeep = $"CLIWRAP_TEST_KEEP_{Guid.NewGuid()}";
            var variableToOverwrite = $"CLIWRAP_TEST_OVERWRITE_{Guid.NewGuid()}";
            var variableToUnset = $"CLIWRAP_TEST_DELETE_{Guid.NewGuid()}";

            Environment.SetEnvironmentVariable(variableToKeep, "foo"); // will be kept
            Environment.SetEnvironmentVariable(variableToOverwrite, "bar"); // will be overwritten
            Environment.SetEnvironmentVariable(variableToUnset, "baz"); // will be unset

            try
            {
                var cmd = Cli.Wrap("dotnet")
                    .WithArguments(a => a
                        .Add(Dummy.Program.FilePath)
                        .Add("print-environment-variables"))
                    .WithEnvironmentVariables(e => e
                        .Set(variableToOverwrite, "new bar")
                        .Set(variableToUnset, null));

                // Act
                var result = await cmd.ExecuteBufferedAsync();
                var stdOutLines = result.StandardOutput.Split(Environment.NewLine);

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
            finally
            {
                Environment.SetEnvironmentVariable(variableToUnset, null);
                Environment.SetEnvironmentVariable(variableToKeep, null);
            }
        }
    }
}
