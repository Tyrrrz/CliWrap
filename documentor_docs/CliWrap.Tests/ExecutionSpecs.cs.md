# Technical Documentation: `CliWrap.Tests/ExecutionSpecs.cs`

## Overview

The `CliWrap.Tests/ExecutionSpecs.cs` file contains integration unit tests for the `CliWrap` library using the **xUnit** framework and **FluentAssertions**. Its primary purpose is to test the core process execution capabilities of `CliWrap`, verifying features such as exit code evaluation, execution timing, process ID retrieval, buffer handling, manual process configuration, async awaiter configuration, cancellation (immediate, delayed, and graceful), and error handling for missing executables.

---

## File Metadata

* **Namespace**: `CliWrap.Tests`
* **Class Name**: `ExecutionSpecs`
* **Target Assembly**: Test Suite for `CliWrap`
* **External Dependencies**:
  * `System`, `System.ComponentModel`, `System.Diagnostics`, `System.Text`, `System.Threading`, `System.Threading.Tasks`
  * `FluentAssertions`
  * `PowerKit.Extensions` (used for `Process.IsRunning` extension method)
  * `Xunit`

---

## Class Architecture

The `ExecutionSpecs` class contains 10 asynchronous and synchronous test methods marked with xUnit's `[Fact]` attribute. Most test methods specify a timeout of 15,000 milliseconds (`[Fact(Timeout = 15000)]`) to prevent hanging during execution tests.

```
ExecutionSpecs
 ├── Basic Execution & Result Specs
 │    ├── I_can_execute_a_command_and_get_its_exit_code_and_execution_time
 │    └── I_can_execute_a_command_and_use_an_implicit_conversion_to_get_its_exit_code
 ├── Process Metadata & Lifecycle Specs
 │    ├── I_can_execute_a_command_and_get_its_associated_process_ID
 │    └── I_can_execute_a_command_with_a_configured_awaiter
 ├── Advanced Process Configuration Specs
 │    ├── I_can_execute_a_command_with_manually_configured_process_settings
 │    └── I_can_execute_a_command_and_not_hang_on_large_stdout_and_stderr
 ├── Cancellation Mechanics Specs
 │    ├── I_can_execute_a_command_and_cancel_it_immediately
 │    ├── I_can_execute_a_command_and_cancel_it_after_a_delay
 │    └── I_can_execute_a_command_and_cancel_it_gracefully_after_a_delay
 └── Exception Handling Specs
      └── I_can_try_to_execute_a_command_and_get_an_error_if_the_target_file_does_not_exist
```

---

## Test Cases Breakdown

### 1. Basic Result Metadata Verification
#### `I_can_execute_a_command_and_get_its_exit_code_and_execution_time`
* **Purpose**: Validates that executing a simple command returns result metadata, specifically an exit code, success flag, and positive run time.
* **Logic**:
  * Wraps `Dummy.Program.FilePath`.
  * Awaits `cmd.ExecuteAsync()`.
  * Verifies `ExitCode == 0`, `IsSuccess == true`, and `RunTime > TimeSpan.Zero`.

### 2. Implicit Result Conversions
#### `I_can_execute_a_command_and_use_an_implicit_conversion_to_get_its_exit_code`
* **Purpose**: Tests implicit type casting from `CommandResult` to `int` and `bool`.
* **Logic**:
  * Executes the command.
  * Casts `result` to `(int)` and asserts it equals `0`.
  * Casts `result` to `(bool)` and asserts it equals `true`.

### 3. Process ID Access
#### `I_can_execute_a_command_and_get_its_associated_process_ID`
* **Purpose**: Verifies that the underlying system process ID can be accessed from the `CommandTask` before awaiting process completion.
* **Logic**:
  * Calls `cmd.ExecuteAsync()` without immediately awaiting it.
  * Asserts `task.ProcessId` is not `0`.
  * Awaits `task` to clean up execution.

### 4. Configured Awaiter (`ConfigureAwait`)
#### `I_can_execute_a_command_with_a_configured_awaiter`
* **Purpose**: Ensures `ExecuteAsync()` task support configuring custom awaiters, such as `.ConfigureAwait(false)`.
* **Logic**:
  * Calls `await cmd.ExecuteAsync().ConfigureAwait(false)`.

### 5. Manual Process Settings Configuration
#### `I_can_execute_a_command_with_manually_configured_process_settings`
* **Purpose**: Verifies that delegates can be passed into `ExecuteAsync` to configure `ProcessStartInfo` and `Process` properties directly.
* **Logic**:
  * Disables result validation using `.WithValidation(CommandResultValidation.None)`.
  * Passes configuration delegates to `ExecuteAsync`:
    * Sets `startInfo.Arguments = "exit 13"`.
    * Sets `process.PriorityBoostEnabled = false`.
  * Asserts that the exit code returned is `13`.

### 6. Large Output Buffer Handling
#### `I_can_execute_a_command_and_not_hang_on_large_stdout_and_stderr`
* **Purpose**: Tests that processes producing large stdout/stderr payloads do not cause deadlocks or hang the system.
* **Logic**:
  * Configures target process with arguments `["generate binary", "--target", "all", "--length", "100000"]`.
  * Awaits execution to ensure completion within the 15-second timeout window.

### 7. Immediate Process Cancellation
#### `I_can_execute_a_command_and_cancel_it_immediately`
* **Purpose**: Validates process termination behavior when a cancellation token is pre-canceled before/at execution start.
* **Logic**:
  * Pre-cancels a `CancellationTokenSource`.
  * Pipes command stdout to a `StringBuilder`.
  * Calls `cmd.ExecuteAsync(cts.Token)`.
  * Asserts an `OperationCanceledException` is thrown containing the token, the process is no longer running (`Process.IsRunning(task.ProcessId) == false`), and standard output does not contain `"Done."`.

### 8. Delayed Process Cancellation
#### `I_can_execute_a_command_and_cancel_it_after_a_delay`
* **Purpose**: Verifies forceful termination when a cancellation token triggers after the process has started running.
* **Logic**:
  * Configures a `CancellationTokenSource` to cancel after 0.2 seconds (`cts.CancelAfter(TimeSpan.FromSeconds(0.2))`).
  * Runs a long-running process (`sleep 00:00:20`).
  * Asserts an `OperationCanceledException` is thrown, the process is killed (`Process.IsRunning == false`), and the output buffer does not contain `"Done."`.

### 9. Graceful Process Cancellation
#### `I_can_execute_a_command_and_cancel_it_gracefully_after_a_delay`
* **Purpose**: Tests graceful cancellation using the second cancellation token parameter (`gracefulCancellationToken`) passed to `ExecuteAsync`.
* **Logic**:
  * Sets up a pipe target using `PipeTarget.Merge` to monitor stdout.
  * When output matching `"Sleeping for"` is received, triggers `cts.CancelAfter(TimeSpan.FromSeconds(0.2))`.
  * Invokes `cmd.ExecuteAsync(CancellationToken.None, cts.Token)`.
  * Asserts that `OperationCanceledException` is thrown, the process is stopped, and stdout contains `"Canceled."` while missing `"Done."`.

### 10. Non-Existent Executable Handling
#### `I_can_try_to_execute_a_command_and_get_an_error_if_the_target_file_does_not_exist`
* **Purpose**: Ensures attempting to start a process pointing to a non-existent executable throws a `Win32Exception` synchronously upon calling `ExecuteAsync()`.
* **Logic**:
  * Wraps non-existent file path `"I_do_not_exist.exe"`.
  * Invokes `cmd.ExecuteAsync()`.
  * Asserts that `Win32Exception` is thrown synchronously (addressing Issue #139).