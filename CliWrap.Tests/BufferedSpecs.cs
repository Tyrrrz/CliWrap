using System;
using System.Threading.Tasks;
using CliWrap.Buffered;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class BufferedSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_run_a_command_with_buffering_and_get_stdout()
    {
        // Arrange
        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo")
                .Add("Hello stdout")
                .Add("--target").Add("stdout")
            );

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Trim().Should().Be("Hello stdout");
        result.StandardError.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_run_a_command_with_buffering_and_get_stderr()
    {
        // Arrange
        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo")
                .Add("Hello stderr")
                .Add("--target").Add("stderr")
            );

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Trim().Should().Be("Hello stderr");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_run_a_command_with_buffering_and_get_stdout_and_stderr()
    {
        // Arrange
        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo")
                .Add("Hello stdout and stderr")
                .Add("--target").Add("all")
            );

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Trim().Should().Be("Hello stdout and stderr");
        result.StandardError.Trim().Should().Be("Hello stdout and stderr");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_run_a_command_with_buffering_and_not_hang_on_large_stdout_and_stderr()
    {
        // Arrange
        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("generate text")
                .Add("--target").Add("all")
                .Add("--length").Add(100_000)
            );

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Should().NotBeNullOrWhiteSpace();
        result.StandardError.Should().NotBeNullOrWhiteSpace();
    }
}