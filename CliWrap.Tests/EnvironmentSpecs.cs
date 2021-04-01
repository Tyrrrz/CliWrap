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
        public async Task Command_can_be_executed_with_custom_environment_variables()
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
        public async Task Command_can_be_executed_with_null_environment_variables_to_unset_them()
        {
            // Arrange

            // Generate unique environment variable names to be used during the test
            var keyToDelete = "CLIWRAP_TEST_DELETE_" + string.Concat(Guid.NewGuid().ToString().Take(5));
            var keyToSet = "CLIWRAP_TEST_KEEP_" + string.Concat(Guid.NewGuid().ToString().Take(5));

            var env = new Dictionary<string, string?>
            {
                // Stting the variable to null, should remove
                // the environment variable
                [keyToDelete] = null,

                [keyToSet] = "value-is-set",
            };

            // Set the variable in the current environment so that we can verify it is removed
            Environment.SetEnvironmentVariable(keyToDelete, "value-is-set");

            // Ensure the variable is not set in the current environment
            Environment.SetEnvironmentVariable(keyToSet, null);

            try
            {
                var cmd = Cli.Wrap("dotnet")
                    .WithArguments(a => a
                        .Add(Dummy.Program.FilePath)
                        .Add("print-environment-variables"))
                    .WithEnvironmentVariables(env);

                // Act
                var result = await cmd.ExecuteBufferedAsync();
                var stdOutLines = result.StandardOutput.Split(Environment.NewLine);
                Array.Sort(stdOutLines);

                // Assert
                stdOutLines.Should()
                    .Contain(new[]
                    {
                        $"[{keyToSet}] = value-is-set",
                    })
                    .And
                    .NotContain(new[]
                    {
                        $"[{keyToDelete}] = value-is-set",
                    });
            }
            finally
            {
                // Remove both environment variables by setting them to null
                Environment.SetEnvironmentVariable(keyToDelete, null);
                Environment.SetEnvironmentVariable(keyToSet, null);
            }
        }
    }
}
