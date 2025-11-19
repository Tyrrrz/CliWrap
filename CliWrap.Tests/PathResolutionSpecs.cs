using System;
using System.IO;
using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.Tests.Utils;
using CliWrap.Tests.Utils.Extensions;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class PathResolutionSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_on_an_executable_using_its_short_name()
    {
        // Arrange
        var cmd = Cli.Wrap("dotnet").WithArguments("--version");

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StandardOutput.Trim().Should().MatchRegex(@"^\d+\.\d+\.\d+$");
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_on_a_script_using_its_short_name()
    {
        Skip.IfNot(
            OperatingSystem.IsWindows(),
            "Path resolution for scripts is only required on Windows."
        );

        // Arrange
        using var dir = TempDir.Create();
        await File.WriteAllTextAsync(Path.Combine(dir.Path, "test-script.cmd"), "@echo hello");

        using (Environment.ExtendPath(dir.Path))
        {
            var cmd = Cli.Wrap("test-script");

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StandardOutput.Trim().Should().Be("hello");
        }
    }
}
