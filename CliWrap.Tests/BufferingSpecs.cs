using System.Threading.Tasks;
using CliWrap.Buffered;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class BufferingSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_buffering_and_get_the_stdout()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["echo", "Hello stdout", "--target", "stdout"]);

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Trim().Should().Be("Hello stdout");
        result.StandardError.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_buffering_and_get_the_stderr()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["echo", "Hello stderr", "--target", "stderr"]);

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Trim().Should().Be("Hello stderr");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_buffering_and_get_the_stdout_and_stderr()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["echo", "Hello stdout and stderr", "--target", "all"]);

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Trim().Should().Be("Hello stdout and stderr");
        result.StandardError.Trim().Should().Be("Hello stdout and stderr");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_buffering_and_not_hang_on_large_stdout_and_stderr()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["generate text", "--target", "all", "--length", "100000"]);

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Should().NotBeNullOrWhiteSpace();
        result.StandardError.Should().NotBeNullOrWhiteSpace();
    }
}
