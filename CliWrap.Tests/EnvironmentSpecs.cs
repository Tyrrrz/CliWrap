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
        public async Task I_can_execute_a_command_with_a_custom_working_directory_path()
        {
            // Arrange
            var workingDirPath = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;

            var cmd = Cli.Wrap("dotnet")
                .WithWorkingDirectory(workingDirPath)
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintWorkingDir));

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.TrimEnd().Should().Be(workingDirPath);
        }

        [Fact(Timeout = 15000)]
        public async Task I_can_execute_a_command_with_custom_environment_variables()
        {
            // Arrange
            var env = new Dictionary<string, string>
            {
                ["foo"] = "bar",
                ["hello"] = "world",
                ["Path"] = "there"
            };

            var expectedOutputLines = env.Select(kvp => $"[{kvp.Key}] = {kvp.Value}").ToArray();

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintEnvVars))
                .WithEnvironmentVariables(env);

            // Act
            var result = await cmd.ExecuteBufferedAsync();
            var stdOutLines = result.StandardOutput.Split(Environment.NewLine);

            // Assert
            stdOutLines.Should().Contain(expectedOutputLines);
        }
    }
}