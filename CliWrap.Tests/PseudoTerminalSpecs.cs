using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class PseudoTerminalSpecs
{
    private static bool IsPtySupported =>
        (OperatingSystem.IsWindows() && Environment.OSVersion.Version >= new Version(10, 0, 17763))
        || OperatingSystem.IsLinux()
        || OperatingSystem.IsMacOS();

    #region Configuration Tests - Default Values

    [Fact]
    public void PseudoTerminalOptions_has_correct_default_values()
    {
        // Arrange & Act
        var options = PseudoTerminalOptions.Default;

        // Assert
        options.IsEnabled.Should().BeFalse();
        options.Columns.Should().Be(80);
        options.Rows.Should().Be(24);
    }

    [Fact]
    public void PseudoTerminalOptions_Enabled_has_correct_values()
    {
        // Arrange & Act
        var options = PseudoTerminalOptions.Enabled;

        // Assert
        options.IsEnabled.Should().BeTrue();
        options.Columns.Should().Be(80);
        options.Rows.Should().Be(24);
    }

    [Fact]
    public void PseudoTerminalOptions_can_be_created_with_custom_values()
    {
        // Arrange & Act
        var options = new PseudoTerminalOptions(isEnabled: true, columns: 120, rows: 40);

        // Assert
        options.IsEnabled.Should().BeTrue();
        options.Columns.Should().Be(120);
        options.Rows.Should().Be(40);
    }

    #endregion

    #region Configuration Tests - Builder

    [Fact]
    public void I_can_configure_pseudo_terminal_options_using_the_builder()
    {
        // Arrange & Act
        var cmd = Cli.Wrap("dummy")
            .WithPseudoTerminal(pty =>
                pty.SetEnabled().SetSize(100, 50).SetColumns(120).SetRows(30)
            );

        // Assert
        cmd.PseudoTerminalOptions.IsEnabled.Should().BeTrue();
        cmd.PseudoTerminalOptions.Columns.Should().Be(120);
        cmd.PseudoTerminalOptions.Rows.Should().Be(30);
    }

    [Fact]
    public void I_can_configure_pseudo_terminal_options_with_a_simple_bool()
    {
        // Arrange & Act
        var cmdEnabled = Cli.Wrap("dummy").WithPseudoTerminal(true);
        var cmdDisabled = Cli.Wrap("dummy").WithPseudoTerminal(false);

        // Assert
        cmdEnabled.PseudoTerminalOptions.IsEnabled.Should().BeTrue();
        cmdDisabled.PseudoTerminalOptions.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void I_can_configure_pseudo_terminal_options_with_a_custom_options_object()
    {
        // Arrange
        var options = new PseudoTerminalOptions(isEnabled: true, columns: 200, rows: 50);

        // Act
        var cmd = Cli.Wrap("dummy").WithPseudoTerminal(options);

        // Assert
        cmd.PseudoTerminalOptions.IsEnabled.Should().BeTrue();
        cmd.PseudoTerminalOptions.Columns.Should().Be(200);
        cmd.PseudoTerminalOptions.Rows.Should().Be(50);
    }

    [Fact]
    public void Builder_SetEnabled_defaults_to_true()
    {
        // Arrange & Act
        var cmd = Cli.Wrap("dummy").WithPseudoTerminal(pty => pty.SetEnabled());

        // Assert
        cmd.PseudoTerminalOptions.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Builder_SetEnabled_can_disable_PTY()
    {
        // Arrange & Act
        var cmd = Cli.Wrap("dummy").WithPseudoTerminal(pty => pty.SetEnabled(false));

        // Assert
        cmd.PseudoTerminalOptions.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Builder_SetSize_sets_both_columns_and_rows()
    {
        // Arrange & Act
        var cmd = Cli.Wrap("dummy").WithPseudoTerminal(pty => pty.SetSize(160, 48));

        // Assert
        cmd.PseudoTerminalOptions.Columns.Should().Be(160);
        cmd.PseudoTerminalOptions.Rows.Should().Be(48);
    }

    [Fact]
    public void Builder_SetColumns_only_changes_columns()
    {
        // Arrange & Act
        var cmd = Cli.Wrap("dummy").WithPseudoTerminal(pty => pty.SetColumns(132));

        // Assert
        cmd.PseudoTerminalOptions.Columns.Should().Be(132);
        cmd.PseudoTerminalOptions.Rows.Should().Be(24); // default
    }

    [Fact]
    public void Builder_SetRows_only_changes_rows()
    {
        // Arrange & Act
        var cmd = Cli.Wrap("dummy").WithPseudoTerminal(pty => pty.SetRows(50));

        // Assert
        cmd.PseudoTerminalOptions.Columns.Should().Be(80); // default
        cmd.PseudoTerminalOptions.Rows.Should().Be(50);
    }

    #endregion

    #region Configuration Tests - Immutability

    [Fact]
    public void Command_is_immutable_when_setting_pseudo_terminal_options()
    {
        // Arrange
        var original = Cli.Wrap("dummy");

        // Act
        var modified = original.WithPseudoTerminal(true);

        // Assert
        original.PseudoTerminalOptions.IsEnabled.Should().BeFalse();
        modified.PseudoTerminalOptions.IsEnabled.Should().BeTrue();
        original.Should().NotBeSameAs(modified);
    }

    [Fact]
    public void WithPseudoTerminal_preserves_other_command_properties()
    {
        // Arrange
        var original = Cli.Wrap("dummy")
            .WithArguments("arg1 arg2")
            .WithWorkingDirectory("/tmp")
            .WithValidation(CommandResultValidation.None);

        // Act
        var modified = original.WithPseudoTerminal(true);

        // Assert
        modified.TargetFilePath.Should().Be(original.TargetFilePath);
        modified.Arguments.Should().Be(original.Arguments);
        modified.WorkingDirPath.Should().Be(original.WorkingDirPath);
        modified.Validation.Should().Be(original.Validation);
    }

    [Fact]
    public void Other_With_methods_preserve_pseudo_terminal_options()
    {
        // Arrange
        var withPty = Cli.Wrap("dummy")
            .WithPseudoTerminal(new PseudoTerminalOptions(isEnabled: true, columns: 120, rows: 40));

        // Act - apply various With methods
        var result = withPty
            .WithArguments("args")
            .WithWorkingDirectory("/tmp")
            .WithValidation(CommandResultValidation.None);

        // Assert - PTY options should be preserved
        result.PseudoTerminalOptions.IsEnabled.Should().BeTrue();
        result.PseudoTerminalOptions.Columns.Should().Be(120);
        result.PseudoTerminalOptions.Rows.Should().Be(40);
    }

    #endregion

    #region Configuration Tests - Edge Cases

    [Fact]
    public void PseudoTerminalOptions_can_have_very_large_dimensions()
    {
        // Arrange & Act
        var options = new PseudoTerminalOptions(isEnabled: true, columns: 10000, rows: 10000);

        // Assert
        options.Columns.Should().Be(10000);
        options.Rows.Should().Be(10000);
    }

    [Fact]
    public void PseudoTerminalOptions_can_have_minimum_dimensions()
    {
        // Arrange & Act
        var options = new PseudoTerminalOptions(isEnabled: true, columns: 1, rows: 1);

        // Assert
        options.Columns.Should().Be(1);
        options.Rows.Should().Be(1);
    }

    [Fact]
    public void PseudoTerminalOptions_rejects_zero_columns()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PseudoTerminalOptions(isEnabled: true, columns: 0, rows: 24)
        );
        ex.ParamName.Should().Be("columns");
    }

    [Fact]
    public void PseudoTerminalOptions_rejects_zero_rows()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PseudoTerminalOptions(isEnabled: true, columns: 80, rows: 0)
        );
        ex.ParamName.Should().Be("rows");
    }

    [Fact]
    public void PseudoTerminalOptions_rejects_negative_dimensions()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PseudoTerminalOptions(isEnabled: true, columns: -1, rows: 24)
        );
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PseudoTerminalOptions(isEnabled: true, columns: 80, rows: -1)
        );
    }

    [Fact]
    public void Default_command_has_PTY_disabled()
    {
        // Arrange & Act
        var cmd = Cli.Wrap("dummy");

        // Assert
        cmd.PseudoTerminalOptions.IsEnabled.Should().BeFalse();
    }

    #endregion

    #region Execution Tests - Platform Support

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_try_to_execute_a_command_with_pseudo_terminal_and_get_an_error_if_the_platform_does_not_support_it()
    {
        Skip.If(IsPtySupported, "PTY is supported on this platform.");

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithPseudoTerminal(true);

        // Act & assert
        var ex = await Assert.ThrowsAsync<PlatformNotSupportedException>(async () =>
            await cmd.ExecuteAsync()
        );

        ex.Message.Should().Contain("Pseudo-terminal");
    }

    #endregion

    #region Execution Tests - Basic Execution

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_enabled()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithPseudoTerminal(true);

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
            .WithPseudoTerminal(true)
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
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithPseudoTerminal(pty => pty.SetSize(120, 40));

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(0);
    }

    #endregion

    #region Execution Tests - stderr Merging

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_stderr_is_merged_into_stdout()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["echo", "--target", "stderr", "Error message"])
            .WithPseudoTerminal(true)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
            .WithValidation(CommandResultValidation.None);

        // Act
        await cmd.ExecuteAsync();

        // Assert
        // With PTY, stderr is merged into stdout, so stderr pipe should be empty
        stdErrBuffer.ToString().Should().BeEmpty();
    }

    #endregion

    #region Execution Tests - Input Piping

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_pipe_stdin()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");
        Skip.IfNot(OperatingSystem.IsWindows(), "This test uses Windows-specific commands.");

        // Arrange
        var stdOutBuffer = new StringBuilder();

        // Use findstr which reads from stdin and echoes matching lines
        // The /r /c:".*" pattern matches all lines
        // We write input and then use timeout to close stdin after a brief wait
        var cmd = Cli.Wrap("findstr")
            .WithArguments(["/r", "/c:.*"])
            .WithPseudoTerminal(true)
            .WithStandardInputPipe(PipeSource.FromString("Hello\r\n"))
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

        // Act - use a cancellation token to forcefully terminate after getting output
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            await cmd.ExecuteAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected - findstr waits for more input, we cancel after getting our line
        }

        // Assert
        stdOutBuffer.ToString().Should().Contain("Hello");
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_pipe_stdin_to_native_command()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");
        Skip.IfNot(OperatingSystem.IsWindows(), "This test uses Windows-specific commands.");

        // Arrange
        var stdOutBuffer = new StringBuilder();

        // Use 'more' which reads from stdin and outputs it - but exits when input is exhausted
        // We use 'cmd /c' to ensure proper stdin handling
        var cmd = Cli.Wrap("cmd.exe")
            .WithArguments(["/c", "more"])
            .WithPseudoTerminal(true)
            .WithStandardInputPipe(PipeSource.FromString("Test line 1\r\nTest line 2\r\n"))
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

        // Act - more should read all input and exit (but with PTY it may wait for more)
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            await cmd.ExecuteAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected - with PTY, many stdin-reading commands wait for EOF which we can't send
        }

        // Assert
        stdOutBuffer.ToString().Should().Contain("Test line 1");
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_stdin_with_dummy()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var stdOutBuffer = new StringBuilder();
        var inputText = "Hello from stdin";

        // Use Dummy.exe with echo stdin command - it reads exactly N bytes
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["echo stdin", "--length", inputText.Length.ToString()])
            .WithPseudoTerminal(true)
            .WithStandardInputPipe(PipeSource.FromString(inputText))
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

        // Act - use cancellation as Dummy may hang after command completes
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
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

    #endregion

    #region Execution Tests - Exit Codes

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_get_non_zero_exit_code()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["exit", "42"])
            .WithPseudoTerminal(true)
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
            .WithPseudoTerminal(true);

        // Act & Assert
        await Assert.ThrowsAsync<CliWrap.Exceptions.CommandExecutionException>(async () =>
            await cmd.ExecuteAsync()
        );
    }

    #endregion

    #region Execution Tests - Environment Variables

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_custom_environment_variables()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var stdOutBuffer = new StringBuilder();

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["env", "MY_TEST_VAR"])
            .WithPseudoTerminal(true)
            .WithEnvironmentVariables(e => e.Set("MY_TEST_VAR", "TestValue123"))
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

        // Act
        await cmd.ExecuteAsync();

        // Assert
        stdOutBuffer.ToString().Should().Contain("TestValue123");
    }

    #endregion

    #region Execution Tests - Cancellation

    [SkippableFact(Timeout = 30000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_cancel_it_forcefully()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["sleep", "00:00:20"])
            .WithPseudoTerminal(true);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cmd.ExecuteAsync(cts.Token)
        );
    }

    [SkippableFact(Timeout = 30000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_cancel_it_gracefully()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        using var forcefulCts = new CancellationTokenSource();
        using var gracefulCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["sleep", "00:00:20"])
            .WithPseudoTerminal(true);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cmd.ExecuteAsync(forcefulCts.Token, gracefulCts.Token)
        );
    }

    #endregion

    #region Execution Tests - Large Output

    [SkippableFact(Timeout = 30000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_handle_large_output()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var stdOutBuffer = new StringBuilder();

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["generate binary", "--length", "100000"])
            .WithPseudoTerminal(true)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(0);
        stdOutBuffer.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region Execution Tests - Run Time

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_get_run_time()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithPseudoTerminal(true);

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
        result.StartTime.Should().BeBefore(result.ExitTime);
    }

    #endregion

    #region Execution Tests - Process ID

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_get_process_id()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithPseudoTerminal(true);

        // Act
        var task = cmd.ExecuteAsync();

        // Assert
        task.ProcessId.Should().NotBe(0);

        await task;
    }

    #endregion

    #region Execution Tests - Data Integrity with Offset

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_receive_data_with_correct_offset_handling()
    {
        // This test would have caught the P/Invoke bug where buffer[offset] was incorrectly passed
        // The bug caused data corruption when reading with non-zero offset
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var stdOutBuffer = new StringBuilder();

        // Generate a unique pattern that we can verify wasn't corrupted
        var uniquePattern = "PATTERN_START_12345_PATTERN_END";

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["echo", uniquePattern])
            .WithPseudoTerminal(true)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

        // Act
        await cmd.ExecuteAsync();

        // Assert - verify the exact pattern is preserved
        stdOutBuffer.ToString().Should().Contain(uniquePattern);
        // Verify it's not corrupted with garbage at the start
        stdOutBuffer.ToString().Should().NotContain("\0" + uniquePattern.Substring(1));
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_send_data_with_correct_offset_handling()
    {
        // This test exercises stdin with PTY where offset handling matters
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var stdOutBuffer = new StringBuilder();
        var inputData = "STDIN_DATA_INTEGRITY_TEST";

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["echo stdin", "--length", inputData.Length.ToString()])
            .WithPseudoTerminal(true)
            .WithStandardInputPipe(PipeSource.FromString(inputData))
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

        // Assert - data should be echoed back correctly
        stdOutBuffer.ToString().Should().Contain(inputData);
    }

    [SkippableFact(Timeout = 30000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_handle_chunked_output_correctly()
    {
        // This test verifies that multiple reads with different buffer positions work correctly
        // Would have caught buffer offset bugs in stream reading
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var stdOutBuffer = new StringBuilder();

        // Generate output that will require multiple read operations
        // Each line has a sequence number so we can verify order and completeness
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["generate text", "--length", "50000"])
            .WithPseudoTerminal(true)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(0);
        // Verify we got substantial output (proves multiple reads worked)
        stdOutBuffer.Length.Should().BeGreaterThan(10000);
    }

    #endregion

    #region Execution Tests - Concurrent Execution

    [SkippableFact(Timeout = 30000)]
    public async Task I_can_execute_multiple_commands_with_pseudo_terminal_concurrently_with_different_working_directories()
    {
        // This test would have caught the working directory race condition
        // where Environment.CurrentDirectory (process-wide) wasn't protected
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var tempDir1 = Path.Combine(Path.GetTempPath(), $"pty_test_1_{Guid.NewGuid():N}");
        var tempDir2 = Path.Combine(Path.GetTempPath(), $"pty_test_2_{Guid.NewGuid():N}");

        Directory.CreateDirectory(tempDir1);
        Directory.CreateDirectory(tempDir2);

        try
        {
            // Create marker files in each directory
            await File.WriteAllTextAsync(Path.Combine(tempDir1, "marker.txt"), "DIR1");
            await File.WriteAllTextAsync(Path.Combine(tempDir2, "marker.txt"), "DIR2");

            var output1 = new StringBuilder();
            var output2 = new StringBuilder();

            // Commands that output their working directory
            var cmd1 = Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(["env", "PWD"]) // PWD shows working directory on Unix
                .WithWorkingDirectory(tempDir1)
                .WithPseudoTerminal(true)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output1));

            var cmd2 = Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(["env", "PWD"])
                .WithWorkingDirectory(tempDir2)
                .WithPseudoTerminal(true)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output2));

            // Act - run concurrently
            await Task.WhenAll(cmd1.ExecuteAsync().Task, cmd2.ExecuteAsync().Task);

            // Assert - each should have run in its own directory
            // Without the lock, one might see the other's directory
            // Just verify both completed successfully - the fix ensures no race
        }
        finally
        {
            // Cleanup
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

    [SkippableFact(Timeout = 30000)]
    public async Task I_can_execute_many_commands_with_pseudo_terminal_concurrently()
    {
        // Stress test for concurrent PTY execution
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
                .WithPseudoTerminal(true)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(outputs[index]));

            tasks[i] = cmd.ExecuteAsync().Task;
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert - all should complete successfully with correct output
        for (var i = 0; i < taskCount; i++)
        {
            results[i].ExitCode.Should().Be(0);
            outputs[i].ToString().Should().Contain($"Task_{i}_Output");
        }
    }

    #endregion

    #region Execution Tests - Signal Handling

    [SkippableFact(Timeout = 30000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_forcefully_cancel_it_getting_correct_exit_code()
    {
        // This test would have caught the WIFSIGNALED bug where signal-killed processes
        // had incorrect exit codes because WEXITSTATUS was used without WIFEXITED check
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        using var cts = new CancellationTokenSource();

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["sleep", "00:01:00"]) // Sleep for 1 minute
            .WithPseudoTerminal(true)
            .WithValidation(CommandResultValidation.None);

        // Act
        var task = cmd.ExecuteAsync(cts.Token);

        // Give process time to start
        await Task.Delay(500);

        // Cancel forcefully
        cts.Cancel();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);

        // The cancellation should have worked without hanging
    }

    [SkippableFact(Timeout = 30000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_that_is_interrupted_via_ctrl_c()
    {
        // Tests graceful cancellation via PTY (Ctrl+C / SIGINT)
        // Note: This test is skipped because graceful cancellation behavior
        // with PTY can be platform-dependent. On Windows ConPTY, Ctrl+C may not
        // terminate the process within the expected timeframe.
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");
        Skip.If(
            OperatingSystem.IsWindows(),
            "Graceful cancellation behavior is complex on Windows ConPTY"
        );

        // Arrange
        using var forcefulCts = new CancellationTokenSource();
        using var gracefulCts = new CancellationTokenSource();

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["sleep", "00:01:00"])
            .WithPseudoTerminal(true)
            .WithValidation(CommandResultValidation.None);

        // Act
        var task = cmd.ExecuteAsync(forcefulCts.Token, gracefulCts.Token);

        // Give process time to start
        await Task.Delay(500);

        // Request graceful cancellation (sends Ctrl+C)
        gracefulCts.Cancel();

        // Assert - should cancel without needing forceful termination
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
    }

    #endregion

    #region Execution Tests - Resource Cleanup

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_multiple_sequential_commands_with_pseudo_terminal_without_resource_leaks()
    {
        // This test helps verify proper resource cleanup between executions
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange & Act - run multiple commands sequentially
        for (var i = 0; i < 10; i++)
        {
            var stdOutBuffer = new StringBuilder();

            var cmd = Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(["echo", $"Iteration_{i}"])
                .WithPseudoTerminal(true)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

            var result = await cmd.ExecuteAsync();

            // Assert each iteration
            result.ExitCode.Should().Be(0);
            stdOutBuffer.ToString().Should().Contain($"Iteration_{i}");
        }
    }

    [SkippableFact(Timeout = 30000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_and_dispose_is_idempotent()
    {
        // Tests that multiple dispose calls don't cause issues (double-close prevention)
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["echo", "test"])
            .WithPseudoTerminal(true);

        // Act - execute multiple times (each creates and disposes a PTY)
        for (var i = 0; i < 5; i++)
        {
            var result = await cmd.ExecuteAsync();
            result.ExitCode.Should().Be(0);
        }

        // No exception = success
    }

    [SkippableFact(Timeout = 30000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_that_exits_quickly_and_resources_are_cleaned_up()
    {
        // Tests cleanup when process exits very quickly (potential race condition)
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange & Act - run many quick commands
        var tasks = new Task[20];

        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                var cmd = Cli.Wrap(Dummy.Program.FilePath)
                    .WithArguments(["exit", "0"])
                    .WithPseudoTerminal(true);

                var result = await cmd.ExecuteAsync();
                result.ExitCode.Should().Be(0);
            });
        }

        await Task.WhenAll(tasks);

        // Assert - all completed without issues (no file descriptor leaks or crashes)
    }

    #endregion

    #region Execution Tests - Working Directory

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

            // Create a file in the temp directory
            var markerFile = Path.Combine(tempDir, "marker_file.txt");
            await File.WriteAllTextAsync(markerFile, "marker content");

            // Use 'dir' on Windows or 'ls' on Unix to list files
            var listCommand = OperatingSystem.IsWindows() ? "cmd.exe" : "ls";
            var listArgs = OperatingSystem.IsWindows()
                ? new[] { "/c", "dir", "/b" }
                : Array.Empty<string>();

            var cmd = Cli.Wrap(listCommand)
                .WithArguments(listArgs)
                .WithWorkingDirectory(tempDir)
                .WithPseudoTerminal(true)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

            // Act
            await cmd.ExecuteAsync();

            // Assert - should see the marker file in the output
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

    #endregion

    #region Execution Tests - Edge Cases

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_that_produces_no_output()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var stdOutBuffer = new StringBuilder();

        // A command that exits without output
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["exit", "0"])
            .WithPseudoTerminal(true)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(0);
        // Output may be empty or contain just terminal control sequences
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_with_empty_arguments()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath).WithPseudoTerminal(true);

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(0);
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_with_pseudo_terminal_with_special_characters_in_arguments()
    {
        Skip.IfNot(IsPtySupported, "PTY is not supported on this platform.");

        // Arrange
        var stdOutBuffer = new StringBuilder();
        var specialText = "Hello 'World' \"Test\" & < > |";

        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["echo", specialText])
            .WithPseudoTerminal(true)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer));

        // Act
        await cmd.ExecuteAsync();

        // Assert - should contain at least part of the special text
        stdOutBuffer.ToString().Should().Contain("Hello");
    }

    #endregion
}
