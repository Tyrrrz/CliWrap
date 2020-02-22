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

        public StreamingSpecs(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_as_an_async_event_stream()
        {
            // Arrange
            const int expectedLinesCount = 1000;

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintLines)
                    .Add(expectedLinesCount));

            // Act
            var stdOutLinesCount = 0;
            var stdErrLinesCount = 0;
            await foreach (var cmdEvent in cmd.ListenAsync())
            {
                cmdEvent
                    .OnStarted(e => _output.WriteLine($"Process started; ID: {e.ProcessId}"))
                    .OnStandardOutput(e =>
                    {
                        _output.WriteLine($"Out> {e.Text}");
                        stdOutLinesCount++;
                    })
                    .OnStandardError(e =>
                    {
                        _output.WriteLine($"Err> {e.Text}");
                        stdErrLinesCount++;
                    })
                    .OnExited(e => _output.WriteLine($"Process exited; Code: {e.ExitCode}"));
            }

            // Assert
            stdOutLinesCount.Should().Be(expectedLinesCount);
            stdErrLinesCount.Should().Be(expectedLinesCount);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_command_as_an_observable_event_stream()
        {
            // Arrange
            const int expectedLinesCount = 1000;

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.FilePath)
                    .Add(Dummy.Program.PrintLines)
                    .Add(expectedLinesCount));

            // Act
            var stdOutLinesCount = 0;
            var stdErrLinesCount = 0;

            await cmd.Observe().ForEachAsync(cmdEvent =>
            {
                cmdEvent
                    .OnStarted(e => _output.WriteLine($"Process started; ID: {e.ProcessId}"))
                    .OnStandardOutput(e =>
                    {
                        _output.WriteLine($"Out> {e.Text}");
                        stdOutLinesCount++;
                    })
                    .OnStandardError(e =>
                    {
                        _output.WriteLine($"Err> {e.Text}");
                        stdErrLinesCount++;
                    })
                    .OnExited(e => _output.WriteLine($"Process exited; Code: {e.ExitCode}"));
            });

            // Assert
            stdOutLinesCount.Should().Be(expectedLinesCount);
            stdErrLinesCount.Should().Be(expectedLinesCount);
        }
    }
}