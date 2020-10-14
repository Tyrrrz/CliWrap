using System.Reactive.Linq;
using System.Threading.Tasks;
using CliWrap.EventStream;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class StreamingSpecs
    {
        [Fact(Timeout = 15000)]
        public async Task I_can_execute_a_command_as_an_async_event_stream()
        {
            // Arrange
            const int expectedLinesCount = 100;

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintRandomLines)
                    .Add(expectedLinesCount));

            // Act
            var stdOutLinesCount = 0;
            var stdErrLinesCount = 0;
            var processHasExited = false;

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
                        processHasExited = true;
                        break;
                }
            }

            // Assert
            stdOutLinesCount.Should().Be(expectedLinesCount);
            stdErrLinesCount.Should().Be(expectedLinesCount);
            processHasExited.Should().BeTrue();
        }

        [Fact(Timeout = 15000)]
        public async Task I_can_execute_a_command_as_an_observable_event_stream()
        {
            // Arrange
            const int expectedLinesCount = 100;

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintRandomLines)
                    .Add(expectedLinesCount));

            // Act
            var stdOutLinesCount = 0;
            var stdErrLinesCount = 0;
            var processHasExited = false;

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
                        processHasExited = true;
                        break;
                }
            });

            // Assert
            stdOutLinesCount.Should().Be(expectedLinesCount);
            stdErrLinesCount.Should().Be(expectedLinesCount);
            processHasExited.Should().BeTrue();
        }
    }
}