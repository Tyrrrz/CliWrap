using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.Tests.Utils;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class PathResolutionSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_on_an_executable_using_its_short_name()
    {
        // Arrange
        var cmd = Cli.Wrap("dotnet")
            .WithArguments("--version");

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Trim().Should().MatchRegex(@"^\d+\.\d+\.\d+$");
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_on_a_script_using_its_short_name()
    {
        Skip.IfNot(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "Path resolution for scripts is only required on Windows."
        );

        // Arrange
        using var dir = TempDir.Create();

        var filePath = Path.Combine(dir.Path, "test-script.cmd");
        await File.WriteAllTextAsync(filePath, "@echo hello");

        var pathValue =
            Environment.GetEnvironmentVariable("PATH") +
            Path.PathSeparator +
            Path.GetDirectoryName(filePath);

        var cmd = Cli.Wrap("test-script");

        // Act
        using (EnvironmentVariable.Set("PATH", pathValue))
        {
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.ExitCode.Should().Be(0);
            result.StandardOutput.Trim().Should().Be("hello");
        }
    }
}