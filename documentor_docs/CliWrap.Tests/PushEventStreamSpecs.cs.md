# Technical Documentation: `PushEventStreamSpecs.cs`

## Overview

The `PushEventStreamSpecs` class is an xUnit unit test suite within the `CliWrap.Tests` namespace. It validates the **push-based event streaming model** offered by CliWrap through the `cmd.Observe()` extension method (powered by Reactive Extensions / `IObservable<CommandEvent>`).

The tests verify that CLI commands executed as reactive event streams behave correctly during standard execution, early unsubscription, handling large output buffers without hanging or deadlocking, and during immediate or delayed cancellations (both standard and graceful).

---

## Dependencies & Namespaces

- **`System` / `System.Diagnostics` / `System.Text`**: Standard .NET framework utilities for process handling, encoding, and diagnostic operations.
- **`System.Reactive.Linq`**: System.Reactive (Rx.NET) operators used to process observables (e.g., `.ToArray()`, `.Take()`, `.FirstAsync()`, `.ForEachAsync()`).
- **`System.Threading` / `System.Threading.Tasks`**: Async/await constructs and `CancellationTokenSource` management.
- **`CliWrap.EventStream`**: Provides `Observe()` extensions and event types (`StartedCommandEvent`, `StandardOutputCommandEvent`, `StandardErrorCommandEvent`, `ExitedCommandEvent`).
- **`FluentAssertions`**: Assertion framework used for fluent verification.
- **`PowerKit.Extensions`**: Extension methods used for process evaluation (`Process.IsRunning`).
- **`Xunit`**: Testing framework; each test uses `[Fact(Timeout = 15000)]` to ensure execution does not exceed 15 seconds.

---

## Test Class Structure

```csharp
namespace CliWrap.Tests;

public class PushEventStreamSpecs
```

All test methods in this class follow a 15-second execution timeout (`[Fact(Timeout = 15000)]`) and use a dummy executable target specified by `Dummy.Program.FilePath`.

---

## Detailed Test Cases

### 1. `I_can_execute_a_command_as_an_event_stream()`

* **Purpose**: Verifies full execution of a command through an observable stream and checks that all command events (`Started`, `StandardOutput`, `StandardError`, `Exited`) are published correctly.
* **Command Setup**: `generate text --target all --lines 1000`
* **Execution**: Calls `await cmd.Observe().ToArray()`.
* **Assertions**:
  * Exactly 1 `StartedCommandEvent` is published with a non-zero `ProcessId`.
  * Exactly 1,000 `StandardOutputCommandEvent` events are captured.
  * Exactly 1,000 `StandardErrorCommandEvent` events are captured.
  * Exactly 1 `ExitedCommandEvent` is published with an `ExitCode` of `0`.

---

### 2. `I_can_execute_a_command_as_an_event_stream_and_unsubscribe_from_it_early()`

* **Purpose**: Tests that unsubscribing from the event stream early (using standard Rx operations) limits event emission without throwing errors.
* **Command Setup**: `generate text --target all --lines 1000`
* **Execution**: Calls `await cmd.Observe().Take(10).ToArray()`.
* **Assertions**:
  * Emitted array contains exactly 10 events.

---

### 3. `I_can_execute_a_command_as_an_event_stream_and_not_hang_on_large_stdout_and_stderr()`

* **Purpose**: Ensures that emitting massive stdout/stderr outputs does not cause buffer overflows or deadlock/hang the event stream process.
* **Command Setup**: `generate text --target all --length 10000000 --lines 1000`
* **Execution**: Calls `await cmd.Observe().ToArray()`.
* **Assertions**: The task completes successfully without hanging.

---

### 4. `I_can_execute_a_command_as_an_event_stream_and_not_hang_on_large_stdout_and_stderr_if_I_unsubscribe_from_it_early()`

* **Purpose**: Ensures that early unsubscription from a process outputting massive amounts of data completes cleanly and does not deadlock background buffer drains.
* **Command Setup**: `generate text --target all --length 10000000 --lines 1000`
* **Execution**: Calls `await cmd.Observe().Take(10).ToArray()`.
* **Assertions**:
  * Emitted array contains exactly 10 events.

---

### 5. `I_can_execute_a_command_as_an_event_stream_and_cancel_it_by_unsubscribing_from_it()`

* **Purpose**: Tests that terminating an Rx subscription kills the underlying operating system process.
* **Command Setup**: `sleep 00:00:20`
* **Execution**:
  1. Awaits `cmd.Observe().OfType<StartedCommandEvent>().FirstAsync()`, which automatically unsubscribes from the stream immediately after receiving the first `StartedCommandEvent`.
  2. Uses `Process.GetProcessById()` and `WaitForExitAsync()` with a 5-second timeout to await process termination.
* **Assertions**:
  * Verifies `Process.IsRunning(startedEvent.ProcessId)` is `false`.

---

### 6. `I_can_execute_a_command_as_an_event_stream_and_cancel_it_immediately()`

* **Purpose**: Verifies that invoking `cmd.Observe()` with an already-canceled `CancellationToken` throws an `OperationCanceledException` immediately.
* **Command Setup**: `sleep 00:00:20` with an instantly canceled `CancellationTokenSource`.
* **Execution**: Iterates through events using `cmd.Observe(cts.Token).ForEachAsync(...)`.
* **Assertions**:
  * Throws `OperationCanceledException` associated with `cts.Token`.

---

### 7. `I_can_execute_a_command_as_an_event_stream_and_cancel_it_after_a_delay()`

* **Purpose**: Tests standard cancellation of an active observable stream after a specified delay (200 milliseconds).
* **Command Setup**: `sleep 00:00:20` with a `CancellationTokenSource` scheduled to cancel after 0.2 seconds (`cts.CancelAfter(TimeSpan.FromSeconds(0.2))`).
* **Execution**: Executes `cmd.Observe(cts.Token).ForEachAsync(...)`.
* **Assertions**:
  * Throws `OperationCanceledException` associated with `cts.Token`.

---

### 8. `I_can_execute_a_command_as_an_event_stream_and_cancel_it_gracefully_after_a_delay()`

* **Purpose**: Validates execution when passing separate tokens for standard cancellation and graceful cancellation to `Observe()`.
* **Command Setup**: `sleep 00:00:20` with a `CancellationTokenSource` configured to cancel after 0.2 seconds passed as the graceful cancellation token argument.
* **Execution**: Calls `cmd.Observe(Encoding.Default, Encoding.Default, CancellationToken.None, cts.Token).ForEachAsync(...)`.
* **Assertions**:
  * Throws `OperationCanceledException` associated with `cts.Token`.

---

## Core Behavior Summary

| Capability Tested | Strategy / Methods Used | Key Assertions / Outcomes |
| :--- | :--- | :--- |
| **Stream Composition** | `cmd.Observe().ToArray()` | Events received in order (`Started` -> `StandardOutput`/`StandardError` -> `Exited`). |
| **Early Unsubscribe** | `.Take(n)` operator on Rx Observable | Processing stops early without locking process resources. |
| **Buffer Stress Handling** | Command configured with large byte/line args | Completes without deadlocks/hanging. |
| **Implicit Process Termination** | Unsubscribing via `.FirstAsync()` | Underlying OS process is terminated. |
| **Explicit Cancellation** | `CancellationToken` passed to `Observe(...)` | Aborts execution and raises `OperationCanceledException`. |
| **Graceful Cancellation** | Graceful token parameter in `Observe(...)` overload | Handles cancellation requests cleanly. |