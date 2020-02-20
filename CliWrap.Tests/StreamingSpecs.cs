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
        public async Task I_can_execute_a_CLI_as_an_async_event_stream()
        {
            // Arrange
            const int expectedLinesCount = 1000;

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.Location)
                    .Add(Dummy.Program.LoopBoth)
                    .Add(expectedLinesCount));

            // Act
            var stdOutLinesCount = 0;
            var stdErrLinesCount = 0;
            await foreach (var cmdEvent in cmd.ListenAsync())
            {
                cmdEvent
                    .OnStart(e => _output.WriteLine($"Process started; ID: {e.ProcessId}"))
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
                    .OnComplete(e => _output.WriteLine($"Process exited; Code: {e.ExitCode}"));
            }

            // Assert
            stdOutLinesCount.Should().Be(expectedLinesCount);
            stdErrLinesCount.Should().Be(expectedLinesCount);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_as_an_observable_event_stream()
        {
            // Arrange
            const int expectedLinesCount = 1000;

            var cmd = Cli.Wrap("dotnet")
                .WithArguments(a => a
                    .Add(Dummy.Program.Location)
                    .Add(Dummy.Program.LoopBoth)
                    .Add(expectedLinesCount));

            // Act
            var stdOutLinesCount = 0;
            var stdErrLinesCount = 0;

            var observable = cmd.Observe();
            _ = observable.ForEachAsync(cmdEvent =>
            {
                cmdEvent
                    .OnStart(e => _output.WriteLine($"Process started; ID: {e.ProcessId}"))
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
                    .OnComplete(e => _output.WriteLine($"Process exited; Code: {e.ExitCode}"));
            });

            await observable.Start();

            // Assert
            stdOutLinesCount.Should().Be(expectedLinesCount);
            stdErrLinesCount.Should().Be(expectedLinesCount);
        }
    }
}