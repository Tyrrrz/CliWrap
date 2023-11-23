using System.Threading.Tasks;
using CliWrap.Buffered;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class ShellSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_in_a_shell()
    {
        // Arrange
        var cmd = Cli.WrapShell("echo hello");

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StandardOutput.Trim().Should().Be("hello");
    }
}
