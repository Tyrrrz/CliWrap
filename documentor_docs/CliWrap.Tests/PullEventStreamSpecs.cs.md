# Technical Documentation: `PullEventStreamSpecs.cs`

## Overview

The `PullEventStreamSpecs.cs` file contains integration unit tests for the `CliWrap` library. Specifically, it tests the **pull-based event stream API** implemented via the `ListenAsync()` extension method. 

These specifications verify that CLI commands executed as pull-based asynchronous event streams (`IAsyncEnumerable<CommandEvent>`) behave as expected under various operational conditions, including:
- Standard complete execution.
- Early stream abandonment (stopping enumeration prior to process completion).
- High-volume standard output (`stdout`) and standard error (`stderr`) streams without hanging.
- Immediate, delayed, and graceful cancellation via `CancellationToken`.

---

## Technical Dependencies

| Namespace / Package | Purpose |
| :--- | :--- |
| `CliWrap.EventStream` | Provides event stream abstractions (`CommandEvent`, `StartedCommandEvent`, `StandardOutputCommandEvent`, `StandardErrorCommandEvent`, `ExitedCommandEvent`) and the `ListenAsync()` extension method. |
| `FluentAssertions` | Provides fluent extension methods for readable test assertions (`Should()`). |
| `Xunit` | Test framework providing `[Fact]` attributes and test timeout configurations. |
| `PowerKit.Extensions` | Provides helper utilities such as process checks (`Process.IsRunning()`). |
| `Dummy.Program` | External target binary path (`Dummy.Program.FilePath`) used to execute test CLI commands. |

---

## Test Suite Configuration

All unit tests in this specification class are annotated with:
```csharp
[Fact(Timeout = 15000)]
```
This sets a hard execution timeout of **15,000 milliseconds (15 seconds)** per test. If a process hangs (e.g., due to stream buffer deadlocks), the test fails automatically upon reaching this threshold.

---

## Test Method Specifications

### 1. `I_can_execute_a_command_as_an_event_stream`

* **Purpose**: Validates complete execution of a process through an event stream from start to finish.
* **Setup**: Invokes `Dummy.Program.FilePath` with arguments `["generate text", "--target", "all", "--lines", "1000"]`.
* **Execution**: Iterates through the event stream using `await foreach` on `cmd.ListenAsync()` and appends all received events to a `List<CommandEvent>`.
* **Assertions**:
  * Contains exactly **1** `StartedCommandEvent` with a non-zero `ProcessId`.
  * Contains exactly **1000** `StandardOutputCommandEvent` instances.
  * Contains exactly **1000** `StandardErrorCommandEvent` instances.
  * Contains exactly **1** `ExitedCommandEvent` with an `ExitCode` equal to `0`.

---

### 2. `I_can_execute_a_command_as_an_event_stream_and_abandon_it_early`

* **Purpose**: Ensures that a consumer can break out of an event stream enumeration early without throwing exceptions or deadlocking.
* **Setup**: Invokes `Dummy.Program.FilePath` with arguments requesting 1,000 lines of output.
* **Execution**: Iterates over `cmd.ListenAsync()` and explicitly breaks out of the loop once 10 events have been processed (`if (++i >= 10) break;`).
* **Assertions**:
  * The iteration counter `i` equals `10`.

---

### 3. `I_can_execute_a_command_as_an_event_stream_and_not_hang_on_large_stdout_and_stderr`

* **Purpose**: Verifies that consuming large amounts of output (10,000,000 characters across 1,000 lines for both `stdout` and `stderr`) does not cause memory or pipe buffer deadlocks.
* **Setup**: Calls `Dummy.Program.FilePath` with arguments `["generate text", "--target", "all", "--length", "10000000", "--lines", "1000"]`.
* **Execution**: Completely drains the `cmd.ListenAsync()` stream using an empty `await foreach` loop.
* **Assertions**:
  * Implicit assertion: The test completes within the 15-second timeout without hanging or timing out.

---

### 4. `I_can_execute_a_command_as_an_event_stream_and_not_hang_on_large_stdout_and_stderr_if_I_abandon_it_early`

* **Purpose**: Verifies that breaking early from a stream producing massive output does not lock up process handle disposal or pipe buffers.
* **Setup**: Configures a command producing 10,000,000 characters across 1,000 lines on both stdout and stderr.
* **Execution**: Iterates over `cmd.ListenAsync()`, incrementing a counter and breaking after receiving 10 events.
* **Assertions**:
  * The iteration counter `i` equals `10`.

---

### 5. `I_can_execute_a_command_as_an_event_stream_and_cancel_it_by_abandoning_it`

* **Purpose**: Tests implicit cancellation and process termination by abandoning the `IAsyncEnumerable` iteration while executing a long-running process (`sleep 00:00:20`).
* **Execution**:
  * Enumerates `cmd.ListenAsync()`.
  * Captures `processId` from `StartedCommandEvent` and immediately breaks out of the loop.
  * Ensures `stdOutEvent.Text` does not contain `"Done."`.
* **Assertions**:
  * Verifies `Process.IsRunning(processId)` is `false` (confirming the underlying process was killed upon abandoning the enumeration).
  * Verifies no exception is thrown to the caller during abandonment.

---

### 6. `I_can_execute_a_command_as_an_event_stream_and_cancel_it_immediately`

* **Purpose**: Validates behavior when an already canceled `CancellationToken` is passed to `cmd.ListenAsync()`.
* **Setup**: Creates a `CancellationTokenSource` and calls `cts.CancelAsync()` prior to execution. Command target is `sleep 00:00:20`.
* **Execution**: Wraps the `await foreach` call over `cmd.ListenAsync(cts.Token)` inside an asynchronous delegate (`act`).
* **Assertions**:
  * Executing `act` throws an `OperationCanceledException`.
  * The exception's `CancellationToken` property matches `cts.Token`.

---

### 7. `I_can_execute_a_command_as_an_event_stream_and_cancel_it_after_a_delay`

* **Purpose**: Tests standard token-based cancellation during active stream consumption after a time delay.
* **Setup**: Creates a `CancellationTokenSource` configured with `cts.CancelAfter(TimeSpan.FromSeconds(0.2))`. Target command is `sleep 00:00:20`.
* **Execution**: Wraps stream enumeration over `cmd.ListenAsync(cts.Token)` inside an asynchronous delegate (`act`).
* **Assertions**:
  * Throws an `OperationCanceledException` matching `cts.Token` once the 0.2-second delay elapses.

---

### 8. `I_can_execute_a_command_as_an_event_stream_and_cancel_it_gracefully_after_a_delay`

* **Purpose**: Tests cancellation using the extended `ListenAsync` overload that accepts separate standard tokens and graceful cancellation tokens.
* **Setup**: Configures `cts.CancelAfter(TimeSpan.FromSeconds(0.2))` on a `sleep 00:00:20` command.
* **Execution**: Invokes overload:
  ```csharp
  cmd.ListenAsync(
      Encoding.Default,
      Encoding.Default,
      CancellationToken.None, // Standard cancellation token
      cts.Token              // Graceful cancellation token
  )
  ```
* **Assertions**:
  * Throws an `OperationCanceledException` associated with `cts.Token` when the graceful cancellation token triggers.

---

## Summary Matrix of Tested Behaviors

| Feature Verified | Method(s) |
| :--- | :--- |
| **Stream Event Verification** | `I_can_execute_a_command_as_an_event_stream` |
| **Early Exit / Loop Break** | `I_can_execute_a_command_as_an_event_stream_and_abandon_it_early`<br>`I_can_execute_a_command_as_an_event_stream_and_not_hang_on_large_stdout_and_stderr_if_I_abandon_it_early` |
| **Buffer Deadlock Prevention** | `I_can_execute_a_command_as_an_event_stream_and_not_hang_on_large_stdout_and_stderr` |
| **Implicit Process Termination** | `I_can_execute_a_command_as_an_event_stream_and_cancel_it_by_abandoning_it` |
| **Explicit Token Cancellation** | `I_can_execute_a_command_as_an_event_stream_and_cancel_it_immediately`<br>`I_can_execute_a_command_as_an_event_stream_and_cancel_it_after_a_delay` |
| **Graceful Token Cancellation** | `I_can_execute_a_command_as_an_event_stream_and_cancel_it_gracefully_after_a_delay` |