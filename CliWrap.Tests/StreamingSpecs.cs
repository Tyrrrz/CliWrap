using System.Globalization;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class StreamingSpecs
    {
        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_asynchronously_read_stdout()
        {
            // Arrange
            const int count = 100_000;

            var cli = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.LoopStdOut)
                    .AddArgument(count));
            }).Streaming();

            // Act
            var stream = cli.ExecuteAsync();

            var i = 0;
            await foreach (var item in stream)
            {
                item.Data.Should().Be(i++.ToString(CultureInfo.InvariantCulture));
            }

            // Assert
            i.Should().Be(count);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_asynchronously_read_stderr()
        {
            // Arrange
            const int count = 100_000;

            var cli = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.LoopStdErr)
                    .AddArgument(count));
            }).Streaming();

            // Act
            var stream = cli.ExecuteAsync();

            var i = 0;
            await foreach (var item in stream)
            {
                item.Data.Should().Be(i++.ToString(CultureInfo.InvariantCulture));
            }

            // Assert
            i.Should().Be(count);
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_asynchronously_read_stdout_and_stderr_at_the_same_time()
        {
            // Arrange
            const int count = 100_000;

            var cli = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.LoopBoth)
                    .AddArgument(count));
            }).Streaming();

            // Act
            var stream = cli.ExecuteAsync();

            var stdOutIndex = 0;
            var stdErrIndex = 0;
            await foreach (var (source, data) in stream)
            {
                if (source == StandardStream.StandardOutput)
                    data.Should().Be(stdOutIndex++.ToString(CultureInfo.InvariantCulture));
                else if (source == StandardStream.StandardError)
                    data.Should().Be(stdErrIndex++.ToString(CultureInfo.InvariantCulture));
            }

            // Assert
            stdOutIndex.Should().Be(count);
            stdErrIndex.Should().Be(count);
        }
    }
}