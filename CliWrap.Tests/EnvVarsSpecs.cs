using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class EnvVarsSpecs
    {
        [Fact]
        public async Task I_can_execute_a_CLI_and_specify_a_set_of_environment_variables()
        {
            // Arrange
            var env = new Dictionary<string, string>
            {
                ["foo"] = "bar",
                ["hello"] = "world"
            };

            var expectedOutputLines = env.Select(kvp => $"[{kvp.Key}] = {kvp.Value}").ToArray();

            var cli = Cli.Wrap("dotnet")
                .SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.PrintEnvVars))
                .SetEnvironmentVariables(env);

            // Act
            var result = await cli.ExecuteBufferedAsync();
            var stdOutLines = result.StandardOutput.Split(Environment.NewLine);

            // Assert
            stdOutLines.Should().Contain(expectedOutputLines);
        }

        [Fact]
        public async Task I_can_execute_a_CLI_and_override_its_default_environment_variables()
        {
            // Arrange
            const string name = "Path";
            const string value = "foo bar";
            var expectedOutputLine = $"[{name}] = {value}";

            var cli = Cli.Wrap("dotnet")
                .SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.PrintEnvVars))
                .SetEnvironmentVariables(env => env[name] = value);

            // Act
            var result = await cli.ExecuteBufferedAsync();
            var stdOutLines = result.StandardOutput.Split(Environment.NewLine);

            // Assert
            stdOutLines.Should().Contain(expectedOutputLine);
        }
    }
}