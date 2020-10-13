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
            var processHasStarted = false;
            var stdOutLinesCount = 0;
            var stdErrLinesCount = 0;
            var processHasExited = false;

            await foreach (var cmdEvent in cmd.ListenAsync())
            {
                switch (cmdEvent)
                {
                    case StartedCommandEvent _:
                        processHasStarted = true;
                        break;
                    case StandardOutputCommandEvent _:
                        stdOutLinesCount++;
                        break;
                    case StandardErrorCommandEvent _:
                        stdErrLinesCount++;
                        break;
                    case ExitedCommandEvent _:
                        processHasExited = true;
                        break;
                }
            }

            // Assert
            processHasStarted.Should().BeTrue();
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
            var processHasStarted = false;
            var stdOutLinesCount = 0;
            var stdErrLinesCount = 0;
            var processHasExited = false;

            await cmd.Observe().ForEachAsync(cmdEvent =>
            {
                switch (cmdEvent)
                {
                    case StartedCommandEvent _:
                        processHasStarted = true;
                        break;
                    case StandardOutputCommandEvent _:
                        stdOutLinesCount++;
                        break;
                    case StandardErrorCommandEvent _:
                        stdErrLinesCount++;
                        break;
                    case ExitedCommandEvent _:
                        processHasExited = true;
                        break;
                }
            });

            // Assert
            processHasStarted.Should().BeTrue();
            stdOutLinesCount.Should().Be(expectedLinesCount);
            stdErrLinesCount.Should().Be(expectedLinesCount);
            processHasExited.Should().BeTrue();
        }
    }
}