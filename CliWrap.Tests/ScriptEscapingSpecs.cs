using System;
using System.IO;
using System.Threading.Tasks;
using AwesomeAssertions;
using CliWrap.Buffered;
using CliWrap.Tests.Utils.Extensions;
using PowerKit;
using Xunit;

namespace CliWrap.Tests;

public class ScriptEscapingSpecs
{
    // Arguments that cmd.exe would otherwise read as operators, variables or escapes
    private static readonly string[] SpecialArguments =
    [
        "a&b",
        "a & echo INJECTED",
        "x|y",
        "<in>",
        "q\"uote",
        "\"quoted\"",
        "%PATH%",
        "100%",
        "caret^",
        "bang!",
        "!PATH!",
        "two words",
        "(paren)",
        "semi;colon",
        "a=b",
        "comma,separated",
        @"trailing\",
        @"C:\dir with space\",
        @"back\\slash",
    ];

    // A script that forwards its arguments to another program, like npm.cmd or gradlew.bat do
    private static async Task<string> CreateForwardingScriptAsync(string dirPath, string fileName)
    {
        var filePath = Path.Combine(dirPath, fileName);

        await File.WriteAllTextAsync(
            filePath,
            $"""@"{Dummy.Program.FilePath}" echo %* --separator ###"""
        );

        return filePath;
    }

    [SkippableTheory(Timeout = 15000)]
    [InlineData("forward.cmd")]
    [InlineData("forward.bat")]
    public async Task I_can_execute_a_command_on_a_script_and_have_its_arguments_passed_verbatim(
        string fileName
    )
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "Script files are only run by cmd.exe on Windows.");

        // Arrange
        using var dir = TempDirectory.Create();
        var filePath = await CreateForwardingScriptAsync(dir.Path, fileName);

        var cmd = Cli.Wrap(filePath).WithArguments(SpecialArguments);

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Trim().Should().Be(string.Join("###", SpecialArguments));
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_on_a_script_using_its_short_name_and_have_its_arguments_passed_verbatim()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "Script files are only run by cmd.exe on Windows.");

        // Arrange
        using var dir = TempDirectory.Create();
        await CreateForwardingScriptAsync(dir.Path, "forward-short.cmd");

        using (Environment.ExtendPath(dir.Path))
        {
            var cmd = Cli.Wrap("forward-short").WithArguments(["a & echo INJECTED", "%PATH%"]);

            // Act
            var result = await cmd.ExecuteBufferedAsync();

            // Assert
            result.StandardOutput.Trim().Should().Be("a & echo INJECTED###%PATH%");
        }
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_on_a_script_with_unescaped_arguments_and_have_them_passed_as_parsed()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "Script files are only run by cmd.exe on Windows.");

        // Arrange
        using var dir = TempDirectory.Create();
        var filePath = await CreateForwardingScriptAsync(dir.Path, "forward.cmd");

        // A raw command line is split into arguments first, so shell operators in it are not run
        var cmd = Cli.Wrap(filePath).WithArguments("one \"two three\" four&echo INJECTED");

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Trim().Should().Be("one###two three###four&echo###INJECTED");
    }
}
