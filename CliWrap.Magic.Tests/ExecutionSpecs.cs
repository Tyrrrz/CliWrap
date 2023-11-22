using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static CliWrap.Magic.Tools;
using Dummy = CliWrap.Tests.Dummy;

namespace CliWrap.Magic.Tests;

public class ExecutionSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_magic_and_get_the_stdout_and_stderr()
    {
        // Arrange
        var cmd = Run(
            Dummy.Program.FilePath,
            new[] { "echo", "Hello stdout and stderr", "--target", "all" }
        );

        // Act
        var (exitCode, stdOut, stdErr) = await cmd;

        // Assert
        exitCode.Should().Be(0);
        stdOut.Should().Be("Hello stdout and stderr");
        stdErr.Should().Be("Hello stdout and stderr");
    }
}
