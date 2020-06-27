using System.Reactive.Linq;
using System.Threading.Tasks;
using CliWrap.EventStream;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace CliWrap.Tests
{
    public class StreamingSpecs
    {
        private readonly ITestOutputHelper _output;

        public StreamingSpecs(ITestOutputHelper output) => _output = output;

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
                    case StartedCommandEvent started:
                        _output.WriteLine($"Process started; ID: {started.ProcessId}");
                        processHasStarted = true;
                        break;
                    case StandardOutputCommandEvent stdOut:
                        _output.WriteLine($"Out> {stdOut.Text}");
                        stdOutLinesCount++;
                        break;
                    case StandardErrorCommandEvent stdErr:
                        _output.WriteLine($"Err> {stdErr.Text}");
                        stdErrLinesCount++;
                        break;
                    case ExitedCommandEvent exited:
                        _output.WriteLine($"Process exited; Code: {exited.ExitCode}");
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
                    case StartedCommandEvent started:
                        _output.WriteLine($"Process started; ID: {started.ProcessId}");
                        processHasStarted = true;
                        break;
                    case StandardOutputCommandEvent stdOut:
                        _output.WriteLine($"Out> {stdOut.Text}");
                        stdOutLinesCount++;
                        break;
                    case StandardErrorCommandEvent stdErr:
                        _output.WriteLine($"Err> {stdErr.Text}");
                        stdErrLinesCount++;
                        break;
                    case ExitedCommandEvent exited:
                        _output.WriteLine($"Process exited; Code: {exited.ExitCode}");
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