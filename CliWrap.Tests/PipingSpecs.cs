using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.Tests.Fixtures;
using CliWrap.Tests.Utils;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class PipingSpecs : IClassFixture<TempOutputFixture>
{
    private readonly TempOutputFixture _tempOutputFixture;

    public PipingSpecs(TempOutputFixture tempOutputFixture) =>
        _tempOutputFixture = tempOutputFixture;

    [Fact(Timeout = 15000)]
    public async Task Stdin_can_be_piped_from_a_stream()
    {
        // Arrange
        await using var stream = new MemoryStream(new byte[]
        {
            0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21
        });

        var cmd = stream | Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo-stdin"));

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Should().Be("Hello world!");
    }

    [Fact(Timeout = 15000)]
    public async Task Stdin_can_be_piped_from_a_file()
    {
        // Arrange
        var filePath = _tempOutputFixture.GetTempFilePath();
        await File.WriteAllTextAsync(filePath, "Hello world!");

        var cmd = PipeSource.FromFile(filePath) | Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo-stdin"));

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Should().Be("Hello world!");
    }

    [Fact(Timeout = 15000)]
    public async Task Stdin_can_be_piped_from_memory()
    {
        // Arrange
        var data = new ReadOnlyMemory<byte>(new byte[]
        {
            0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21
        });

        var cmd = data | Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo-stdin"));

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Should().Be("Hello world!");
    }

    [Fact(Timeout = 15000)]
    public async Task Stdin_can_be_piped_from_a_byte_array()
    {
        // Arrange
        var data = new byte[]
        {
            0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21
        };

        var cmd = data | Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo-stdin"));

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Should().Be("Hello world!");
    }

    [Fact(Timeout = 15000)]
    public async Task Stdin_can_be_piped_from_a_string()
    {
        // Arrange
        var cmd = "Hello world!" | Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo-stdin"));

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Should().Be("Hello world!");
    }

    [Fact(Timeout = 15000)]
    public async Task Stdin_can_be_piped_from_stdout_of_another_command()
    {
        // Arrange
        var cmd =
            Cli.Wrap("dotnet").WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("generate-binary")
                .Add("--length").Add(1_000_000)) |
            Cli.Wrap("dotnet").WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("print-stdin-length"));

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Should().Be("1000000");
    }

    [Fact(Timeout = 15000)]
    public async Task Stdin_can_be_piped_from_stdout_of_another_command_in_a_chain()
    {
        // Arrange
        var cmd =
            "Hello world" |
            Cli.Wrap("dotnet").WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo-stdin")) |
            Cli.Wrap("dotnet").WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo-stdin")
                .Add("--length").Add(5)) |
            Cli.Wrap("dotnet").WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("print-stdin-length"));

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Should().Be("5");
    }

    [Fact(Timeout = 15000)]
    public async Task Stdout_can_be_piped_into_a_stream()
    {
        // Arrange
        await using var stream = new MemoryStream();

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("generate-binary")
                .Add("--length").Add(1_000_000)) | stream;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stream.Length.Should().Be(1_000_000);
    }

    [Fact(Timeout = 15000)]
    public async Task Stdout_can_be_piped_into_a_file()
    {
        // Arrange
        var filePath = _tempOutputFixture.GetTempFilePath();

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("generate-binary")
                .Add("--length").Add(1_000_000)) | PipeTarget.ToFile(filePath);

        // Act
        await cmd.ExecuteAsync();

        // Assert
        File.Exists(filePath).Should().BeTrue();
        new FileInfo(filePath).Length.Should().Be(1_000_000);
    }

    [Fact(Timeout = 15000)]
    public async Task Stdout_can_be_piped_into_a_string_builder()
    {
        // Arrange
        var buffer = new StringBuilder();

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo")
                .Add("Hello world!")) | buffer;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        buffer.ToString().Should().Be("Hello world!");
    }

    [Fact(Timeout = 15000)]
    public async Task Stdout_can_be_piped_into_a_delegate()
    {
        // Arrange
        var stdOutLinesCount = 0;

        void HandleStdOut(string s) => stdOutLinesCount++;

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("generate-text-lines")
                .Add("--count").Add(100)) | HandleStdOut;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLinesCount.Should().Be(100);
    }

    [Fact(Timeout = 15000)]
    public async Task Stdout_can_be_piped_into_an_async_delegate()
    {
        // Arrange
        var stdOutLinesCount = 0;

        async Task HandleStdOutAsync(string s)
        {
            await Task.Yield();
            stdOutLinesCount++;
        }

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("generate-text-lines")
                .Add("--count").Add(100)) | HandleStdOutAsync;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLinesCount.Should().Be(100);
    }

    [Fact(Timeout = 15000)]
    public async Task Stdout_and_stderr_can_be_piped_into_separate_streams()
    {
        // Arrange
        await using var stdOut = new MemoryStream();
        await using var stdErr = new MemoryStream();

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("generate-binary")
                .Add("--target").Add("all")
                .Add("--length").Add(1_000_000)) | (stdOut, stdErr);

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOut.Length.Should().Be(1_000_000);
        stdErr.Length.Should().Be(1_000_000);
    }

    [Fact(Timeout = 15000)]
    public async Task Stdout_and_stderr_can_be_piped_into_separate_string_builders()
    {
        // Arrange
        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo").Add("Hello world!")
                .Add("--target").Add("all")) | (stdOutBuffer, stdErrBuffer);

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutBuffer.ToString().Should().Be("Hello world!");
        stdErrBuffer.ToString().Should().Be("Hello world!");
    }

    [Fact(Timeout = 15000)]
    public async Task Stdout_and_stderr_can_be_piped_into_separate_delegates()
    {
        // Arrange
        var stdOutLinesCount = 0;
        var stdErrLinesCount = 0;

        void HandleStdOut(string s) => stdOutLinesCount++;
        void HandleStdErr(string s) => stdErrLinesCount++;

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("generate-text-lines")
                .Add("--target").Add("all")
                .Add("--count").Add(100)) | (HandleStdOut, HandleStdErr);

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLinesCount.Should().Be(100);
        stdErrLinesCount.Should().Be(100);
    }

    [Fact(Timeout = 15000)]
    public async Task Stdout_and_stderr_can_be_piped_into_separate_async_delegates()
    {
        // Arrange
        var stdOutLinesCount = 0;
        var stdErrLinesCount = 0;

        async Task HandleStdOutAsync(string s)
        {
            await Task.Yield();
            stdOutLinesCount++;
        }

        async Task HandleStdErrAsync(string s)
        {
            await Task.Yield();
            stdErrLinesCount++;
        }

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("generate-text-lines")
                .Add("--target").Add("all")
                .Add("--count").Add(100)) | (HandleStdOutAsync, HandleStdErrAsync);

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLinesCount.Should().Be(100);
        stdErrLinesCount.Should().Be(100);
    }

    [Fact(Timeout = 15000)]
    public async Task Stdout_can_be_piped_into_a_merged_target()
    {
        // Arrange
        await using var stream1 = new MemoryStream();
        await using var stream2 = new MemoryStream();
        await using var stream3 = new MemoryStream();

        var pipeTarget = PipeTarget.Merge(
            PipeTarget.ToStream(stream1),
            PipeTarget.ToStream(stream2),
            PipeTarget.ToStream(stream3)
        );

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("generate-binary")
                .Add("--length").Add(100_000)) | pipeTarget;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stream1.Length.Should().Be(100_000);
        stream2.Length.Should().Be(100_000);
        stream3.Length.Should().Be(100_000);
        stream1.ToArray().Should().Equal(stream2.ToArray());
        stream2.ToArray().Should().Equal(stream3.ToArray());
    }

    [Fact(Timeout = 15000)]
    public async Task Stdout_can_be_piped_into_a_hierarchy_of_merged_targets()
    {
        // Arrange
        await using var stream1 = new MemoryStream();
        await using var stream2 = new MemoryStream();
        await using var stream3 = new MemoryStream();
        await using var stream4 = new MemoryStream();

        var pipeTarget = PipeTarget.Merge(
            PipeTarget.ToStream(stream1),
            PipeTarget.Merge(
                PipeTarget.ToStream(stream2),
                PipeTarget.Merge(
                    PipeTarget.ToStream(stream3),
                    PipeTarget.ToStream(stream4)
                )
            )
        );

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("generate-binary")
                .Add("--length").Add(10_000)) | pipeTarget;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stream1.Length.Should().Be(10_000);
        stream2.Length.Should().Be(10_000);
        stream3.Length.Should().Be(10_000);
        stream4.Length.Should().Be(10_000);
        stream1.ToArray().Should().Equal(stream2.ToArray());
        stream2.ToArray().Should().Equal(stream3.ToArray());
        stream3.ToArray().Should().Equal(stream4.ToArray());
    }

    [Fact(Timeout = 15000)]
    public async Task Stdout_can_be_piped_into_multiple_streams_simultaneously_with_large_buffer()
    {
        // https://github.com/Tyrrrz/CliWrap/issues/81

        // Arrange
        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("generate-binary")
                .Add("--length").Add(1_000_000)
                .Add("--buffer").Add(100_000)); // needs to be >= BufferSizes.Stream to fail

        // Act

        // Run without merging to get the expected byte array (random seed is constant)
        await using var unmergedStream = new MemoryStream();
        await (cmd | PipeTarget.ToStream(unmergedStream)).ExecuteAsync();

        // Run with merging to check if it's the same
        await using var mergedStream1 = new MemoryStream();
        await using var mergedStream2 = new MemoryStream();
        await (cmd | PipeTarget.Merge(
                PipeTarget.ToStream(mergedStream1),
                PipeTarget.ToStream(mergedStream2))
            ).ExecuteAsync();

        // Assert
        unmergedStream.Length.Should().Be(1_000_000);
        mergedStream1.ToArray().Should().Equal(unmergedStream.ToArray());
        mergedStream2.ToArray().Should().Equal(unmergedStream.ToArray());
    }

    [Fact(Timeout = 15000)]
    public async Task Stdout_can_be_piped_into_a_stream_while_also_buffering()
    {
        // Arrange
        await using var stream = new MemoryStream();

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("generate-text")
                .Add("--length").Add(100_000)) | stream;

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        stream.Seek(0, SeekOrigin.Begin);
        using var streamReader = new StreamReader(stream);
        var streamContent = await streamReader.ReadToEndAsync();

        // Assert
        result.StandardOutput.Should().Be(streamContent);
    }

    [Fact(Timeout = 15000)]
    public async Task Stdout_can_be_piped_into_into_a_delegate_while_also_buffering()
    {
        // https://github.com/Tyrrrz/CliWrap/issues/75

        // Arrange
        var delegateLines = new List<string>();
        void HandleStdOut(string s) => delegateLines.Add(s);

        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("generate-text-lines")
                .Add("--count").Add(100)) | HandleStdOut;

        // Act
        var result = await cmd.ExecuteBufferedAsync();
        var resultLines = result.StandardOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        // Assert
        delegateLines.Should().Equal(resultLines);
    }

    [Fact(Timeout = 15000)]
    public async Task Not_piping_stdin_into_a_command_that_expects_it_does_not_deadlock()
    {
        // Arrange
        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo-stdin"));

        // Act
        await cmd.ExecuteAsync();
    }

    [Fact(Timeout = 15000)]
    public async Task Piping_empty_stdin_into_a_command_that_expects_it_does_not_deadlock()
    {
        // Arrange
        await using var stream = new MemoryStream();

        var cmd = stream | Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo-stdin"));

        // Act
        await cmd.ExecuteAsync();
    }

    [Fact(Timeout = 15000)]
    public async Task Piping_large_stdin_into_a_command_that_simultaneously_writes_stdout_and_stderr_does_not_deadlock()
    {
        // https://github.com/Tyrrrz/CliWrap/issues/61

        // Arrange
        await using var stream = new RandomStream(10_000_000);

        var cmd = stream | Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo-stdin"));

        // Act
        await cmd.ExecuteAsync();
    }

    [Fact(Timeout = 15000)]
    public async Task Piping_infinite_stdin_into_a_command_that_only_reads_it_partially_does_not_deadlock()
    {
        // https://github.com/Tyrrrz/CliWrap/issues/74

        // Arrange
        await using var stream = new RandomStream(-1);

        var cmd = stream | Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo-stdin")
                .Add("--length").Add(10_000_000));

        // Act
        await cmd.ExecuteAsync();
    }

    [Fact(Timeout = 15000)]
    public async Task Piping_unresolvable_stdin_into_a_command_that_does_not_read_it_does_not_deadlock()
    {
        // https://github.com/Tyrrrz/CliWrap/issues/74

        // Arrange
        await using var stream = new UnresolvableEmptyStream();

        var cmd = stream | Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo-stdin")
                .Add("--length").Add(0));

        // Act
        await cmd.ExecuteAsync();
    }

    [Fact(Timeout = 15000)]
    public async Task Piping_unresolvable_and_uncancellable_stdin_into_a_command_that_does_not_read_it_does_not_deadlock()
    {
        // https://github.com/Tyrrrz/CliWrap/issues/74

        // Arrange
        await using var stream = new UnresolvableEmptyStream(false);

        var cmd = stream | Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(Dummy.Program.FilePath)
                .Add("echo-stdin")
                .Add("--length").Add(0));

        // Act
        await cmd.ExecuteAsync();
    }
}