using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static CliWrap.Magic.Spells;
using Dummy = CliWrap.Tests.Dummy;

namespace CliWrap.Magic.Tests;

public class ExecutionSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_magic_and_get_the_exit_code()
    {
        // Arrange
        var cmd = _(Dummy.Program.FilePath);

        // Act
        int result = await cmd;

        // Assert
        result.Should().Be(0);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_magic_and_verify_that_it_succeeded()
    {
        // Arrange
        var cmd = _(Dummy.Program.FilePath);

        // Act
        bool result = await cmd;

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_magic_and_get_the_stdout()
    {
        // Arrange
        var cmd = _(Dummy.Program.FilePath, "echo", "Hello stdout");

        // Act
        string result = await cmd;

        // Assert
        result.Trim().Should().Be("Hello stdout");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_magic_and_get_the_stdout_and_stderr()
    {
        // Arrange
        var cmd = _(Dummy.Program.FilePath, "echo", "Hello stdout and stderr", "--target", "all");

        // Act
        var (exitCode, stdOut, stdErr) = await cmd;

        // Assert
        exitCode.Should().Be(0);
        stdOut.Trim().Should().Be("Hello stdout and stderr");
        stdErr.Trim().Should().Be("Hello stdout and stderr");
    }
}
