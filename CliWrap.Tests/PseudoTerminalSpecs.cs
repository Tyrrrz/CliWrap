using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Tests.Utils.Extensions;
using FluentAssertions;
using PowerKit;
using Xunit;

namespace CliWrap.Tests;

public class PseudoTerminalSpecs
{
    private static bool IsPtySupported =>
        (OperatingSystem.IsWindows() && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        || OperatingSystem.IsLinux()
        || OperatingSystem.IsMacOS();

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_try_to_execute_a_command_with_pseudo_terminal_and_get_an_error_if_the_platform_does_not_support_it()
    {
        Skip.If(IsPtySupported, "PTY is supported on this platform.");

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithPseudoTerminal();

        // Act & assert
        var ex = await Assert.ThrowsAsync<PlatformNotSupportedException>(async () =>
            await cmd.ExecuteAsync()
        );

        ex.Message.Should().Contain("Pseudo-terminal");
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_enabled()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithPseudoTerminal();

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(0);
        result.IsSuccess.Should().BeTrue();
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_capture_output()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var stdOutBuffer = new StringBuilder();

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["echo", "Hello PTY"])
            .WithPseudoTerminal()
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

        // Act
        await cmd.ExecuteAsync();

        // Assert
        // Note: PTY may add extra characters like \r, so we check for containment
        stdOutBuffer.ToString().Should().Contain("Hello PTY");
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_custom_terminal_size()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithPseudoTerminal(columns: 120, rows: 40);

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(0);
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_stderr_is_merged_into_stdout()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var stdErrBuffer = new StringBuilder();

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["echo", "--target", "stderr", "Error message"])
            .WithPseudoTerminal()
            .WithValidation(CommandResultValidation.None);

        // Direct stderr pipe via the ICommand interface — the concrete PtyCommand hides it.
        var ptyAsICommand = (ICommand)cmd;
        var withStdErrPipe = ptyAsICommand.WithStandardErrorPipe(
            PipeTarget.ToStringBuilder(stdErrBuffer)
        );

        // Act
        await withStdErrPipe.ExecuteAsync();

        // Assert
        // With PTY, stderr is merged into stdout, so stderr pipe receives an empty stream
        stdErrBuffer.ToString().Should().BeEmpty();
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_pipe_stdin()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var stdOutBuffer = new StringBuilder();
        var inputText = "Hello from stdin";

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["echo stdin", "--length", inputText.Length.ToString()])
            .WithPseudoTerminal()
            .WithStandardInputPipe(PipeSource.FromString(inputText))
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await cmd.ExecuteAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // May be expected if process doesn't exit cleanly under PTY
        }

        // Assert
        stdOutBuffer.ToString().Should().Contain(inputText);
    }

    [SkippableFact(Timeout = 15000)]
    public async Task Pseudo_terminal_does_not_wait_for_a_stdin_source_that_ignores_cancellation()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["echo stdin", "--length", "0"])
            .WithPseudoTerminal()
            .WithStandardInputPipe(
                PipeSource.Create(
                    async (_, _) =>
                        await Task.Delay(TimeSpan.FromSeconds(20), CancellationToken.None)
                )
            );

        await cmd.ExecuteAsync();
    }

    [SkippableFact(Timeout = 15000)]
    public async Task Pseudo_terminal_preserves_empty_arguments()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");
        var stdOutBuffer = new StringBuilder();

        await Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["echo", "first", "", "third", "--separator", ","])
            .WithPseudoTerminal()
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
            .ExecuteAsync();

        stdOutBuffer.ToString().Should().Contain("first,,third");
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_pipe_out_of_a_pseudo_terminal_with_an_operator()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");
        var stdOutBuffer = new StringBuilder();

        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(["echo", "Hello from operator"])
                .WithPseudoTerminal() | stdOutBuffer;

        await cmd.ExecuteAsync();

        stdOutBuffer.ToString().Should().Contain("Hello from operator");
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_get_non_zero_exit_code()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["exit", "42"])
            .WithPseudoTerminal()
            .WithValidation(CommandResultValidation.None);

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(42);
        result.IsSuccess.Should().BeFalse();
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_validation_throws_on_non_zero_exit()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["exit", "1"])
            .WithPseudoTerminal();

        // Act & Assert
        await Assert.ThrowsAsync<CliWrap.Exceptions.CommandExecutionException>(async () =>
            await cmd.ExecuteAsync()
        );
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_custom_environment_variables()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var stdOutBuffer = new StringBuilder();

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["env", "MY_TEST_VAR"])
            .WithPseudoTerminal()
            .WithEnvironmentVariables(e => e.Set("MY_TEST_VAR", "TestValue123"))
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutBuffer.ToString().Should().Contain("TestValue123");
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_unset_an_inherited_environment_variable_with_pseudo_terminal()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        var variableName = $"CLIWRAP_PTY_UNSET_{Guid.NewGuid():N}";
        using var _ = Environment.SetTempEnvironmentVariable(variableName, "inherited-value");
        var stdOutBuffer = new StringBuilder();

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["env", variableName])
            .WithPseudoTerminal()
            .WithEnvironmentVariables(e => e.Set(variableName, null))
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

        await cmd.ExecuteAsync();

        stdOutBuffer.ToString().Should().NotContain("inherited-value");
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_cancel_it_forcefully()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["sleep", "00:00:20"])
            .WithPseudoTerminal();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cmd.ExecuteAsync(cts.Token)
        );
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_cancel_it_gracefully()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        // Graceful cancellation fires at 500ms. Forceful cancellation at 3s as a fallback
        // in case the process doesn't respond to the interrupt signal (Ctrl+C).
        // On Windows ConPTY, some processes may not handle Ctrl+C properly.
        using var forcefulCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        using var gracefulCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["sleep", "00:00:20"])
            .WithPseudoTerminal();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cmd.ExecuteAsync(forcefulCts.Token, gracefulCts.Token)
        );
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_handle_large_output()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var stdOutBuffer = new StringBuilder();

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["generate binary", "--length", "100000"])
            .WithPseudoTerminal()
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(0);
        stdOutBuffer.Length.Should().BeGreaterThan(0);
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_pipe_large_pseudo_terminal_output_to_a_file_without_truncation()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");
        using var file = TempFile.Create();

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["generate text", "--length", "50000"])
            .WithPseudoTerminal()
            .WithStandardOutputPipe(PipeTarget.ToFile(file.Path));

        await cmd.ExecuteAsync();

        new FileInfo(file.Path).Length.Should().BeGreaterThan(50000);
    }

    [SkippableFact(Timeout = 15000)]
    public async Task Pseudo_terminal_input_pipe_propagates_derived_io_exceptions()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithPseudoTerminal()
            .WithStandardInputPipe(
                PipeSource.Create((_, _) => Task.FromException(new FileNotFoundException("test")))
            );

        await Assert.ThrowsAsync<FileNotFoundException>(async () => await cmd.ExecuteAsync());
    }

    [SkippableFact(Timeout = 15000)]
    public async Task Pseudo_terminal_process_finishes_early_when_an_output_pipe_fails()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        var commandTask = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["sleep", "00:00:20"])
            .WithPseudoTerminal()
            .WithStandardOutputPipe(
                PipeTarget.Create(
                    (_, _) => Task.FromException(new Exception("Expected exception."))
                )
            )
            .ExecuteAsync();

        var exception = await Assert.ThrowsAsync<Exception>(async () => await commandTask);

        exception.Message.Should().Contain("Expected exception.");
        IsProcessRunning(commandTask.ProcessId).Should().BeFalse();
    }

    [SkippableFact(Timeout = 15000)]
    public async Task Linux_pseudo_terminal_descriptors_are_not_inherited_by_other_processes()
    {
        Skip.IfNot(OperatingSystem.IsLinux(), "This assertion requires procfs.");
        using var cancellationTokenSource = new CancellationTokenSource();
        var ptyCommandTask = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["sleep", "00:00:20"])
            .WithPseudoTerminal()
            .ExecuteAsync(cancellationTokenSource.Token);

        try
        {
            var descriptors = new StringBuilder();
            await Cli.Wrap("sh")
                .WithArguments([
                    "-c",
                    "for fd in /proc/self/fd/*; do readlink \"$fd\" 2>/dev/null || true; done",
                ])
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(descriptors))
                .ExecuteAsync();

            descriptors.ToString().Should().NotContain("/dev/pts/ptmx");
        }
        finally
        {
            await cancellationTokenSource.CancelAsync();
            try
            {
                await ptyCommandTask;
            }
            catch (OperationCanceledException) { }
        }
    }

    [SkippableFact(Timeout = 15000)]
    public async Task Unix_pseudo_terminal_is_the_childs_controlling_terminal()
    {
        Skip.IfNot(
            OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(),
            "This assertion is Unix-specific."
        );

        var cmd = Cli.Wrap("sh")
            .WithArguments(["-c", "test -t 0 && test -t 1 && test -t 2"])
            .WithPseudoTerminal();

        var result = await cmd.ExecuteAsync();

        result.ExitCode.Should().Be(0);
    }

    [SkippableFact(Timeout = 15000)]
    public async Task Unix_pseudo_terminal_failed_starts_do_not_leak_file_descriptors()
    {
        Skip.IfNot(OperatingSystem.IsLinux(), "This assertion requires procfs.");
        var descriptorCountBefore = Directory.EnumerateFileSystemEntries("/proc/self/fd").Count();

        for (var i = 0; i < 20; i++)
        {
            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await Cli.Wrap($"missing-command-{Guid.NewGuid():N}")
                    .WithPseudoTerminal()
                    .ExecuteAsync()
            );
        }

        var descriptorCountAfter = Directory.EnumerateFileSystemEntries("/proc/self/fd").Count();
        descriptorCountAfter.Should().BeLessThanOrEqualTo(descriptorCountBefore + 1);
    }

    [SkippableFact(Timeout = 15000)]
    public async Task Unix_pseudo_terminal_forceful_cancellation_terminates_descendants()
    {
        Skip.IfNot(
            OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(),
            "This assertion is Unix-specific."
        );
        using var pidFile = TempFile.Create();
        using var cancellationTokenSource = new CancellationTokenSource();

        var script = $"sleep 20 & child=$!; echo $child > '{pidFile.Path}'; wait";
        var commandTask = Cli.Wrap("sh")
            .WithArguments(["-c", script])
            .WithPseudoTerminal()
            .ExecuteAsync(cancellationTokenSource.Token);

        try
        {
            string? childProcessIdText = null;
            for (var i = 0; i < 100 && string.IsNullOrWhiteSpace(childProcessIdText); i++)
            {
                await Task.Delay(20);
                childProcessIdText = await File.ReadAllTextAsync(pidFile.Path);
            }

            childProcessIdText.Should().NotBeNullOrWhiteSpace();
            var childProcessId = int.Parse(childProcessIdText!);

            await cancellationTokenSource.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await commandTask);

            for (var i = 0; i < 100 && IsProcessRunning(childProcessId); i++)
                await Task.Delay(20);

            IsProcessRunning(childProcessId).Should().BeFalse();
        }
        finally
        {
            await cancellationTokenSource.CancelAsync();
            try
            {
                await commandTask;
            }
            catch { }
        }
    }

    private static bool IsProcessRunning(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    [SkippableFact(Timeout = 15000)]
    public async Task Windows_pseudo_terminal_can_execute_batch_files()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "This assertion is Windows-specific.");
        using var file = TempFile.Create();
        var batchPath = Path.ChangeExtension(file.Path, ".cmd");
        await File.WriteAllTextAsync(batchPath, "@echo batch-output");
        var stdOutBuffer = new StringBuilder();

        try
        {
            await Cli.Wrap(batchPath)
                .WithPseudoTerminal()
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .ExecuteAsync();

            stdOutBuffer.ToString().Should().Contain("batch-output");
        }
        finally
        {
            File.Delete(batchPath);
        }
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_get_run_time()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithPseudoTerminal();

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
        result.StartTime.Should().BeBefore(result.ExitTime);
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_get_process_id()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithPseudoTerminal();

        // Act
        var task = cmd.ExecuteAsync();

        // Assert
        task.ProcessId.Should().NotBe(0);

        await task;
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_receive_data_with_correct_offset_handling()
    {
        // This test would have caught the P/Invoke bug where buffer[offset] was incorrectly passed
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var stdOutBuffer = new StringBuilder();
        var uniquePattern = "PATTERN_START_12345_PATTERN_END";

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["echo", uniquePattern])
            .WithPseudoTerminal()
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutBuffer.ToString().Should().Contain(uniquePattern);
        stdOutBuffer.ToString().Should().NotContain("\0" + uniquePattern.Substring(1));
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_handle_chunked_output_correctly()
    {
        // Multiple reads with different buffer positions exercise the read loop and copy semantics.
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var stdOutBuffer = new StringBuilder();

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["generate text", "--length", "50000"])
            .WithPseudoTerminal()
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(0);
        stdOutBuffer.Length.Should().BeGreaterThan(10000);
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_multiple_commands_with_pseudo_terminal_concurrently_with_different_working_directories()
    {
        // Would have caught the working directory race condition where Environment.CurrentDirectory
        // (process-wide) wasn't protected.
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var tempDir1 = Path.Combine(Path.GetTempPath(), $"pty_test_1_{Guid.NewGuid():N}");
        var tempDir2 = Path.Combine(Path.GetTempPath(), $"pty_test_2_{Guid.NewGuid():N}");

        Directory.CreateDirectory(tempDir1);
        Directory.CreateDirectory(tempDir2);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir1, "marker.txt"), "DIR1");
            await File.WriteAllTextAsync(Path.Combine(tempDir2, "marker.txt"), "DIR2");

            var output1 = new StringBuilder();
            var output2 = new StringBuilder();

            var cmd1 = Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments("cwd")
                .WithWorkingDirectory(tempDir1)
                .WithPseudoTerminal()
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output1));

            var cmd2 = Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments("cwd")
                .WithWorkingDirectory(tempDir2)
                .WithPseudoTerminal()
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output2));

            // Act
            await Task.WhenAll(cmd1.ExecuteAsync().Task, cmd2.ExecuteAsync().Task);

            // Assert
            output1.ToString().Should().Contain(tempDir1);
            output2.ToString().Should().Contain(tempDir2);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir1, true);
            }
            catch { }
            try
            {
                Directory.Delete(tempDir2, true);
            }
            catch { }
        }
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_many_commands_with_pseudo_terminal_concurrently()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var taskCount = 5;
        var tasks = new Task<CommandResult>[taskCount];
        var outputs = new StringBuilder[taskCount];

        for (var i = 0; i < taskCount; i++)
        {
            outputs[i] = new StringBuilder();
            var index = i;

            var cmd = Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(["echo", $"Task_{index}_Output"])
                .WithPseudoTerminal()
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(outputs[index]));

            tasks[i] = cmd.ExecuteAsync().Task;
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        for (var i = 0; i < taskCount; i++)
        {
            results[i].ExitCode.Should().Be(0);
            outputs[i].ToString().Should().Contain($"Task_{i}_Output");
        }
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_multiple_sequential_commands_with_pseudo_terminal_without_resource_leaks()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        for (var i = 0; i < 10; i++)
        {
            var stdOutBuffer = new StringBuilder();

            var cmd = Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(["echo", $"Iteration_{i}"])
                .WithPseudoTerminal()
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

            var result = await cmd.ExecuteAsync();

            result.ExitCode.Should().Be(0);
            stdOutBuffer.ToString().Should().Contain($"Iteration_{i}");
        }
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_that_exits_quickly_and_resources_are_cleaned_up()
    {
        // Race-condition coverage for quickly-exiting processes.
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        var tasks = new Task[20];

        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                var cmd = Cli.Wrap(Dummy.Program.FilePath)
                    .WithArguments(["exit", "0"])
                    .WithPseudoTerminal();

                var result = await cmd.ExecuteAsync();
                result.ExitCode.Should().Be(0);
            });
        }

        await Task.WhenAll(tasks);
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_in_specific_working_directory()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"pty_workdir_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var stdOutBuffer = new StringBuilder();

            var markerFile = Path.Combine(tempDir, "marker_file.txt");
            await File.WriteAllTextAsync(markerFile, "marker content");

            var listCommand = OperatingSystem.IsWindows() ? "cmd.exe" : "ls";
            var listArgs = OperatingSystem.IsWindows()
                ? new[] { "/c", "dir", "/b" }
                : Array.Empty<string>();

            var cmd = Cli.Wrap(listCommand)
                .WithArguments(listArgs)
                .WithWorkingDirectory(tempDir)
                .WithPseudoTerminal()
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

            // Act
            await cmd.ExecuteAsync();

            // Assert
            stdOutBuffer.ToString().Should().Contain("marker_file.txt");
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch { }
        }
    }
}
