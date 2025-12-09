using System;
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
    public void PseudoTerminalOptions_can_have_zero_dimensions()
    {
        // Note: Zero dimensions may cause issues at runtime, but the configuration allows it
        // Arrange & Act
        var options = new PseudoTerminalOptions(isEnabled: true, columns: 0, rows: 0);

        // Assert
        options.Columns.Should().Be(0);
        options.Rows.Should().Be(0);
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
}
