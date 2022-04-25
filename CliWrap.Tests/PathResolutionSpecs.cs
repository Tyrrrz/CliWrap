using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.Tests.Fixtures;
using CliWrap.Tests.Utils;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class PathResolutionSpecs : IClassFixture<TempOutputFixture>
{
    private readonly TempOutputFixture _tempOutput;

    public PathResolutionSpecs(TempOutputFixture tempOutput) =>
        _tempOutput = tempOutput;

    [Fact(Timeout = 15000)]
    public async Task Command_can_be_executed_on_an_executable_using_its_short_name()
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
    public async Task Command_can_be_executed_on_a_script_using_its_short_name()
    {
        // Only relevant to Windows
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        // Arrange
        var filePath = _tempOutput.GetTempFilePath("test-script.cmd");
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