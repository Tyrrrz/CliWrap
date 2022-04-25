using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.Exceptions;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace CliWrap.Tests;

public class ValidationSpecs
{
    private readonly ITestOutputHelper _testOutput;

    public ValidationSpecs(ITestOutputHelper testOutput) => _testOutput = testOutput;

    [Fact(Timeout = 15000)]
    public async Task Command_execution_throws_if_the_process_exits_with_a_non_zero_exit_code()
    {
        // Arrange
        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("exit")
                .Add("--code").Add(1)
            );

        // Act & assert
        var ex = await Assert.ThrowsAsync<CommandExecutionException>(async () => await cmd.ExecuteAsync());

        ex.ExitCode.Should().Be(1);
        ex.Command.Should().BeEquivalentTo(cmd);

        _testOutput.WriteLine(ex.Message);
    }

    [Fact(Timeout = 15000)]
    public async Task Buffered_command_execution_throws_a_detailed_error_if_the_process_exits_with_a_non_zero_exit_code()
    {
        // Arrange
        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("exit")
                .Add("--code").Add(1)
            );

        // Act & assert
        var ex = await Assert.ThrowsAsync<CommandExecutionException>(async () => await cmd.ExecuteBufferedAsync());

        ex.Message.Should().Contain("Standard error");
        ex.ExitCode.Should().Be(1);
        ex.Command.Should().BeEquivalentTo(cmd);

        _testOutput.WriteLine(ex.Message);
    }

    [Fact(Timeout = 15000)]
    public async Task Command_result_validation_can_be_disabled()
    {
        // Arrange
        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("exit")
                .Add("--code").Add(1)
            )
            .WithValidation(CommandResultValidation.None);

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(1);
    }
}