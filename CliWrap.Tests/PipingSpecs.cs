using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.Tests.Utils;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class PipingSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdin_from_an_async_anonymous_source()
    {
        // Arrange
        var source = PipeSource.Create(
            async (destination, cancellationToken) =>
                await destination.WriteAsync("Hello world!"u8.ToArray(), cancellationToken)
        );

        var cmd = source | Cli.Wrap(Dummy.Program.FilePath).WithArguments("echo stdin");

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Trim().Should().Be("Hello world!");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdin_from_a_sync_anonymous_source()
    {
        // Arrange
        var source = PipeSource.Create(destination => destination.Write("Hello world!"u8));

        var cmd = source | Cli.Wrap(Dummy.Program.FilePath).WithArguments("echo stdin");

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Trim().Should().Be("Hello world!");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdin_from_a_stream()
    {
        // Arrange
        using var stream = new MemoryStream("Hello world!"u8.ToArray());

        var cmd = stream | Cli.Wrap(Dummy.Program.FilePath).WithArguments("echo stdin");

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Trim().Should().Be("Hello world!");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdin_from_a_file()
    {
        // Arrange
        using var file = TempFile.Create();
        File.WriteAllText(file.Path, "Hello world!");

        var cmd =
            PipeSource.FromFile(file.Path)
            | Cli.Wrap(Dummy.Program.FilePath).WithArguments("echo stdin");

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Trim().Should().Be("Hello world!");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdin_from_memory()
    {
        // Arrange
        var data = new ReadOnlyMemory<byte>("Hello world!"u8.ToArray());

        var cmd = data | Cli.Wrap(Dummy.Program.FilePath).WithArguments("echo stdin");

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Trim().Should().Be("Hello world!");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdin_from_a_byte_array()
    {
        // Arrange
        var data = "Hello world!"u8.ToArray();

        var cmd = data | Cli.Wrap(Dummy.Program.FilePath).WithArguments("echo stdin");

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Trim().Should().Be("Hello world!");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdin_from_a_string()
    {
        // Arrange
        var cmd = "Hello world!" | Cli.Wrap(Dummy.Program.FilePath).WithArguments("echo stdin");

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Trim().Should().Be("Hello world!");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdin_from_another_command()
    {
        // Arrange
        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "generate binary", "--length", "100000" })
            | Cli.Wrap(Dummy.Program.FilePath).WithArguments("print length stdin");

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Trim().Should().Be("100000");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdin_from_a_chain_of_commands()
    {
        // Arrange
        var cmd =
            "Hello world"
            | Cli.Wrap(Dummy.Program.FilePath).WithArguments("echo stdin")
            | Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "echo stdin", "--length", "5" })
            | Cli.Wrap(Dummy.Program.FilePath).WithArguments("print length stdin");

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        result.StandardOutput.Trim().Should().Be("5");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdout_into_an_async_anonymous_target()
    {
        // Arrange
        using var stream = new MemoryStream();

        var target = PipeTarget.Create(
            async (origin, cancellationToken) =>
                // ReSharper disable once AccessToDisposedClosure
                await origin.CopyToAsync(stream, cancellationToken)
        );

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "generate binary", "--length", "100000" }) | target;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stream.Length.Should().Be(100_000);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdout_into_a_sync_anonymous_target()
    {
        // Arrange
        using var stream = new MemoryStream();

        var target = PipeTarget.Create(
            origin =>
                // ReSharper disable once AccessToDisposedClosure
                origin.CopyTo(stream)
        );

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "generate binary", "--length", "100000" }) | target;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stream.Length.Should().Be(100_000);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdout_into_a_stream()
    {
        // Arrange
        using var stream = new MemoryStream();

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "generate binary", "--length", "100000" }) | stream;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stream.Length.Should().Be(100_000);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdout_into_a_file()
    {
        // Arrange
        using var file = TempFile.Create();

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "generate binary", "--length", "100000" })
            | PipeTarget.ToFile(file.Path);

        // Act
        await cmd.ExecuteAsync();

        // Assert
        File.Exists(file.Path).Should().BeTrue();
        new FileInfo(file.Path).Length.Should().Be(100_000);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdout_into_a_string_builder()
    {
        // Arrange
        var buffer = new StringBuilder();

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath).WithArguments(new[] { "echo", "Hello world!" })
            | buffer;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        buffer.ToString().Trim().Should().Be("Hello world!");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdout_into_an_async_delegate()
    {
        // Arrange
        var stdOutLinesCount = 0;

        async Task HandleStdOutAsync(string line)
        {
            await Task.Yield();
            stdOutLinesCount++;
        }

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "generate text", "--lines", "100" }) | HandleStdOutAsync;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLinesCount.Should().Be(100);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdout_into_an_async_delegate_with_cancellation()
    {
        // Arrange
        var stdOutLinesCount = 0;

        async Task HandleStdOutAsync(string line, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken);
            stdOutLinesCount++;
        }

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "generate text", "--lines", "100" }) | HandleStdOutAsync;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLinesCount.Should().Be(100);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdout_into_a_sync_delegate()
    {
        // Arrange
        var stdOutLinesCount = 0;

        void HandleStdOut(string line) => stdOutLinesCount++;

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "generate text", "--lines", "100" }) | HandleStdOut;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLinesCount.Should().Be(100);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdout_and_stderr_into_separate_streams()
    {
        // Arrange
        using var stdOut = new MemoryStream();
        using var stdErr = new MemoryStream();

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "generate binary", "--target", "all", "--length", "100000" })
            | (stdOut, stdErr);

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOut.Length.Should().Be(100_000);
        stdErr.Length.Should().Be(100_000);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdout_and_stderr_into_string_builders()
    {
        // Arrange
        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "echo", "Hello world!", "--target", "all" })
            | (stdOutBuffer, stdErrBuffer);

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutBuffer.ToString().Trim().Should().Be("Hello world!");
        stdErrBuffer.ToString().Trim().Should().Be("Hello world!");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdout_and_stderr_into_separate_async_delegates()
    {
        // Arrange
        var stdOutLinesCount = 0;
        var stdErrLinesCount = 0;

        async Task HandleStdOutAsync(string line)
        {
            await Task.Yield();
            stdOutLinesCount++;
        }

        async Task HandleStdErrAsync(string line)
        {
            await Task.Yield();
            stdErrLinesCount++;
        }

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "generate text", "--target", "all", "--lines", "100" })
            | (HandleStdOutAsync, HandleStdErrAsync);

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLinesCount.Should().Be(100);
        stdErrLinesCount.Should().Be(100);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdout_and_stderr_into_separate_async_delegates_with_cancellation()
    {
        // Arrange
        var stdOutLinesCount = 0;
        var stdErrLinesCount = 0;

        async Task HandleStdOutAsync(string line, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken);
            stdOutLinesCount++;
        }

        async Task HandleStdErrAsync(string line, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken);
            stdErrLinesCount++;
        }

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "generate text", "--target", "all", "--lines", "100" })
            | (HandleStdOutAsync, HandleStdErrAsync);

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLinesCount.Should().Be(100);
        stdErrLinesCount.Should().Be(100);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdout_and_stderr_into_separate_sync_delegates()
    {
        // Arrange
        var stdOutLinesCount = 0;
        var stdErrLinesCount = 0;

        void HandleStdOut(string line) => stdOutLinesCount++;
        void HandleStdErr(string line) => stdErrLinesCount++;

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "generate text", "--target", "all", "--lines", "100" })
            | (HandleStdOut, HandleStdErr);

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutLinesCount.Should().Be(100);
        stdErrLinesCount.Should().Be(100);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdout_into_multiple_targets()
    {
        // Arrange
        using var stream1 = new MemoryStream();
        using var stream2 = new MemoryStream();
        using var stream3 = new MemoryStream();

        var target = PipeTarget.Merge(
            PipeTarget.ToStream(stream1),
            PipeTarget.ToStream(stream2),
            PipeTarget.ToStream(stream3)
        );

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "generate binary", "--length", "100000" }) | target;

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
    public async Task I_can_execute_a_command_and_pipe_the_stdout_into_multiple_targets_and_not_hang_on_large_stdout_if_one_of_the_targets_throws_an_exception()
    {
        // https://github.com/Tyrrrz/CliWrap/issues/212

        // Arrange
        var target = PipeTarget.Merge(
            PipeTarget.ToStream(Stream.Null),
            PipeTarget.ToDelegate(_ => throw new Exception("Expected exception."))
        );

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "generate binary", "--length", "100_000" }) | target;

        // Act & assert
        var ex = await Assert.ThrowsAnyAsync<Exception>(async () => await cmd.ExecuteAsync());
        ex.Message.Should().Contain("Expected exception.");
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdout_into_multiple_hierarchical_targets()
    {
        // Arrange
        using var stream1 = new MemoryStream();
        using var stream2 = new MemoryStream();
        using var stream3 = new MemoryStream();
        using var stream4 = new MemoryStream();

        var target = PipeTarget.Merge(
            PipeTarget.ToStream(stream1),
            PipeTarget.Merge(
                PipeTarget.ToStream(stream2),
                PipeTarget.Merge(PipeTarget.ToStream(stream3), PipeTarget.ToStream(stream4))
            )
        );

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "generate binary", "--length", "100000" }) | target;

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stream1.Length.Should().Be(100_000);
        stream2.Length.Should().Be(100_000);
        stream3.Length.Should().Be(100_000);
        stream4.Length.Should().Be(100_000);
        stream1.ToArray().Should().Equal(stream2.ToArray());
        stream2.ToArray().Should().Equal(stream3.ToArray());
        stream3.ToArray().Should().Equal(stream4.ToArray());
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_pipe_the_stdout_into_multiple_streams_with_a_large_buffer()
    {
        // https://github.com/Tyrrrz/CliWrap/issues/81

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(
                new[]
                {
                    "generate binary",
                    "--length",
                    "1000000",
                    // Buffer needs to be >= BufferSizes.Stream to fail
                    "--buffer",
                    "100000"
                }
            );

        // Act
        using var mergedStream1 = new MemoryStream();
        using var mergedStream2 = new MemoryStream();
        await (
            cmd
            | PipeTarget.Merge(
                PipeTarget.ToStream(mergedStream1),
                PipeTarget.ToStream(mergedStream2)
            )
        ).ExecuteAsync();

        // Assert

        // Run without merging to get the expected byte array (random seed is constant)
        using var unmergedStream = new MemoryStream();
        await (cmd | PipeTarget.ToStream(unmergedStream)).ExecuteAsync();

        unmergedStream.Length.Should().Be(1_000_000);
        mergedStream1.ToArray().Should().Equal(unmergedStream.ToArray());
        mergedStream2.ToArray().Should().Equal(unmergedStream.ToArray());
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_buffering_and_also_pipe_the_stdout_into_a_stream()
    {
        // Arrange
        using var stream = new MemoryStream();

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "generate text", "--length", "100000" }) | stream;

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        stream.Seek(0, SeekOrigin.Begin);
        using var streamReader = new StreamReader(stream);
        var streamContent = await streamReader.ReadToEndAsync();

        result.StandardOutput.Should().Be(streamContent);
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_buffering_and_also_pipe_the_stdout_into_a_sync_delegate()
    {
        // https://github.com/Tyrrrz/CliWrap/issues/75

        // Arrange
        var delegateLines = new List<string>();
        void HandleStdOut(string line) => delegateLines.Add(line);

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "generate text", "--lines", "100" }) | HandleStdOut;

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        delegateLines
            .Should()
            .Equal(
                result.StandardOutput.Split(
                    Environment.NewLine,
                    StringSplitOptions.RemoveEmptyEntries
                )
            );
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_get_an_error_if_the_pipe_source_throws_an_exception()
    {
        // Arrange
        var cmd =
            PipeSource.FromFile("non-existing-file.txt")
            | Cli.Wrap(Dummy.Program.FilePath).WithArguments("echo stdin");

        // Act & assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await cmd.ExecuteAsync());
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_get_an_error_if_the_pipe_target_throws_an_exception()
    {
        // Arrange
        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "generate binary", "--length", "100_000" })
            | PipeTarget.ToFile("non-existing-directory/file.txt");

        // Act & assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await cmd.ExecuteAsync());
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_not_hang_if_the_process_expects_stdin_but_none_is_provided()
    {
        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithArguments("echo stdin");

        // Act
        await cmd.ExecuteAsync();
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_not_hang_if_the_process_expects_stdin_but_empty_data_is_provided()
    {
        // Arrange
        var cmd =
            Array.Empty<byte>() | Cli.Wrap(Dummy.Program.FilePath).WithArguments("echo stdin");

        // Act
        await cmd.ExecuteAsync();
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_not_hang_if_the_process_only_partially_consumes_stdin()
    {
        // https://github.com/Tyrrrz/CliWrap/issues/74

        // Arrange
        var random = new Random(1234567);

        var source = PipeSource.Create(
            async (destination, cancellationToken) =>
            {
                var buffer = new byte[256];
                while (true)
                {
                    random.NextBytes(buffer);
                    await destination.WriteAsync(buffer, cancellationToken);
                }

                // ReSharper disable once FunctionNeverReturns
            }
        );

        var cmd =
            source
            | Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "echo stdin", "--length", "100000" });

        // Act & assert
        await cmd.ExecuteAsync();
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_not_hang_if_the_process_does_not_consume_stdin()
    {
        // https://github.com/Tyrrrz/CliWrap/issues/74

        // Arrange
        var source = PipeSource.Create(
            async (_, cancellationToken) =>
            {
                // Not infinite, but long enough
                await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
            }
        );

        var cmd =
            source
            | Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "echo stdin", "--length", "0" });

        // Act & assert
        await cmd.ExecuteAsync();
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_not_hang_if_the_process_does_not_consume_stdin_even_if_the_source_cannot_be_canceled()
    {
        // https://github.com/Tyrrrz/CliWrap/issues/74

        // Arrange
        var source = PipeSource.Create(
            async (_, _) =>
                // Not infinite, but long enough
                await Task.Delay(TimeSpan.FromSeconds(20), CancellationToken.None)
        );

        var cmd =
            source
            | Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(new[] { "echo stdin", "--length", "0" });

        // Act & assert
        await cmd.ExecuteAsync();
    }

    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_and_not_hang_on_large_stdin_while_also_writing_stdout()
    {
        // https://github.com/Tyrrrz/CliWrap/issues/61

        // Arrange
        var random = new Random(1234567);
        var bytesRemaining = 100_000;

        var source = PipeSource.Create(
            async (destination, cancellationToken) =>
            {
                var buffer = new byte[256];
                while (bytesRemaining > 0)
                {
                    random.NextBytes(buffer);

                    var count = Math.Min(bytesRemaining, buffer.Length);
                    await destination.WriteAsync(buffer.AsMemory()[..count], cancellationToken);

                    bytesRemaining -= count;
                }
            }
        );

        var cmd = source | Cli.Wrap(Dummy.Program.FilePath).WithArguments("echo stdin");

        // Act & assert
        await cmd.ExecuteAsync();
    }
}
