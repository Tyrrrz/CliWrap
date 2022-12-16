using System;
using System.Threading.Tasks;
using CliWrap.Buffered;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class BufferedSpecs
{
    [Fact(Timeout = 15000)]
    public async Task Command_can_be_executed_with_buffering_which_yields_a_result_containing_stdout()
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
        result.ExitCode.Should().Be(0);
        result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
        result.StandardOutput.Trim().Should().Be("Hello stdout");
        result.StandardError.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task Command_can_be_executed_with_buffering_which_yields_a_result_containing_stderr()
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
        result.ExitCode.Should().Be(0);
        result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Trim().Should().Be("Hello stderr");
    }

    [Fact(Timeout = 15000)]
    public async Task Command_can_be_executed_with_buffering_which_yields_a_result_containing_stdout_and_stderr()
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
        result.ExitCode.Should().Be(0);
        result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
        result.StandardOutput.Trim().Should().Be("Hello stdout and stderr");
        result.StandardError.Trim().Should().Be("Hello stdout and stderr");
    }

    [Fact(Timeout = 15000)]
    public async Task Buffered_command_execution_does_not_deadlock_on_large_stdout_and_stderr()
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