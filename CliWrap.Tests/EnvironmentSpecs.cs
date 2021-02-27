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
            var env = new Dictionary<string, string>
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
    }
}