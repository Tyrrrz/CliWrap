using System.Reactive.Linq;
using System.Threading.Tasks;
using CliWrap.EventStream;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class EventStreamSpecs
{
    [Fact(Timeout = 15000)]
    public async Task Command_can_be_executed_as_a_pull_event_stream()
    {
        // Arrange
        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("generate text")
                .Add("--target").Add("all")
                .Add("--lines").Add(100)
            );

        // Act
        var stdOutLinesCount = 0;
        var stdErrLinesCount = 0;
        var isExited = false;

        await foreach (var cmdEvent in cmd.ListenAsync())
        {
            switch (cmdEvent)
            {
                case StartedCommandEvent started:
                    started.ProcessId.Should().NotBe(0);
                    break;
                case StandardOutputCommandEvent stdOut:
                    stdOut.Text.Should().NotBeNullOrEmpty();
                    stdOutLinesCount++;
                    break;
                case StandardErrorCommandEvent stdErr:
                    stdErr.Text.Should().NotBeNullOrEmpty();
                    stdErrLinesCount++;
                    break;
                case ExitedCommandEvent exited:
                    exited.ExitCode.Should().Be(0);
                    isExited = true;
                    break;
            }
        }

        // Assert
        stdOutLinesCount.Should().Be(100);
        stdErrLinesCount.Should().Be(100);
        isExited.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public async Task Command_can_be_executed_as_a_push_event_stream()
    {
        // Arrange
        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("generate text")
                .Add("--target").Add("all")
                .Add("--lines").Add(100)
            );

        // Act
        var stdOutLinesCount = 0;
        var stdErrLinesCount = 0;
        var isExited = false;

        await cmd.Observe().ForEachAsync(cmdEvent =>
        {
            switch (cmdEvent)
            {
                case StartedCommandEvent started:
                    started.ProcessId.Should().NotBe(0);
                    break;
                case StandardOutputCommandEvent stdOut:
                    stdOut.Text.Should().NotBeNullOrEmpty();
                    stdOutLinesCount++;
                    break;
                case StandardErrorCommandEvent stdErr:
                    stdErr.Text.Should().NotBeNullOrEmpty();
                    stdErrLinesCount++;
                    break;
                case ExitedCommandEvent exited:
                    exited.ExitCode.Should().Be(0);
                    isExited = true;
                    break;
            }
        });

        // Assert
        stdOutLinesCount.Should().Be(100);
        stdErrLinesCount.Should().Be(100);
        isExited.Should().BeTrue();
    }
}