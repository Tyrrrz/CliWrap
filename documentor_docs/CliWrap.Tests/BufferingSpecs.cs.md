# Technical Documentation: `CliWrap.Tests/BufferingSpecs.cs`

## Overview

The `BufferingSpecs` class contains a suite of automated unit/integration tests using xUnit and FluentAssertions. The purpose of this test file is to verify the buffered execution functionality of the CliWrap library, provided via the `CliWrap.Buffered` namespace and specifically through the `ExecuteBufferedAsync()` extension method.

The tests ensure that standard output (`stdout`) and standard error (`stderr`) streams are correctly captured, formatted, deconstructed, converted, handled under high load, and canceled appropriately (immediately, after a delay, or gracefully).

---

## File Context and Dependencies

* **File Path:** `CliWrap.Tests/BufferingSpecs.cs`
* **Namespace:** `CliWrap.Tests`

### Dependencies
* `System`: Core System types (e.g., `TimeSpan`, `OperationCanceledException`).
* `System.Text`: Text encoding primitives (`Encoding`).
* `System.Threading`: Threading primitives (`CancellationToken`, `CancellationTokenSource`).
* `System.Threading.Tasks`: Asynchronous programming constructs (`Task`).
* `CliWrap.Buffered`: Provides `ExecuteBufferedAsync` extension methods and buffered result handling.
* `FluentAssertions`: Fluent assertion methods for test verification (`Should()`).
* `Xunit`: Test framework providing the `[Fact]` attribute.

---

## Test Class Summary

### `BufferingSpecs`
A public class that groups test specifications focused on verifying buffered execution behavior when calling an external executable (represented by `Dummy.Program.FilePath`).

Every test method in this class is attributed with `[Fact(Timeout = 15000)]`, ensuring that any test taking longer than 15 seconds will automatically fail due to a timeout constraint.

---

## Detailed Test Cases

### 1. `I_can_execute_a_command_and_get_its_stdout`
* **Purpose:** Verifies that invoking `ExecuteBufferedAsync()` on a command outputting to `stdout` captures the `stdout` stream content while leaving `stderr` empty.
* **Arguments Passed:** `["echo", "Hello stdout", "--target", "stdout"]`
* **Assertions:**
  * `result.StandardOutput.Trim()` equals `"Hello stdout"`.
  * `result.StandardError` is empty.

### 2. `I_can_execute_a_command_and_get_its_stderr`
* **Purpose:** Verifies that invoking `ExecuteBufferedAsync()` on a command outputting to `stderr` captures the `stderr` stream content while leaving `stdout` empty.
* **Arguments Passed:** `["echo", "Hello stderr", "--target", "stderr"]`
* **Assertions:**
  * `result.StandardOutput` is empty.
  * `result.StandardError.Trim()` equals `"Hello stderr"`.

### 3. `I_can_execute_a_command_and_get_its_stdout_and_stderr`
* **Purpose:** Verifies that execution correctly captures both `stdout` and `stderr` simultaneously when output is sent to both streams.
* **Arguments Passed:** `["echo", "Hello stdout and stderr", "--target", "all"]`
* **Assertions:**
  * `result.StandardOutput.Trim()` equals `"Hello stdout and stderr"`.
  * `result.StandardError.Trim()` equals `"Hello stdout and stderr"`.

### 4. `I_can_execute_a_command_and_use_an_implicit_conversion_to_get_its_stdout`
* **Purpose:** Validates that the object returned by `ExecuteBufferedAsync()` supports implicit conversion directly to a `string`, which extracts the command's standard output.
* **Arguments Passed:** `["echo", "Hello stdout", "--target", "stdout"]`
* **Execution:** Assigns `await cmd.ExecuteBufferedAsync()` directly to a variable of type `string`.
* **Assertions:**
  * `result.Trim()` equals `"Hello stdout"`.

### 5. `I_can_execute_a_command_and_use_deconstruction_to_get_its_stdout_and_stderr`
* **Purpose:** Tests tuple deconstruction on the buffered execution result object to extract exit code, `stdout`, and `stderr` directly into individual variables.
* **Arguments Passed:** `["echo", "Hello stdout and stderr", "--target", "all"]`
* **Execution:** `var (exitCode, stdOut, stdErr) = await cmd.ExecuteBufferedAsync();`
* **Assertions:**
  * `exitCode` equals `0`.
  * `stdOut.Trim()` equals `"Hello stdout and stderr"`.
  * `stdErr.Trim()` equals `"Hello stdout and stderr"`.

### 6. `I_can_execute_a_command_and_not_hang_on_large_stdout_and_stderr`
* **Purpose:** Ensures stream reading buffers large volumes of output without causing deadlocks or buffer overflows.
* **Arguments Passed:** `["generate text", "--target", "all", "--length", "100000"]`
* **Assertions:**
  * `result.StandardOutput` is not null or whitespace.
  * `result.StandardError` is not null or whitespace.

### 7. `I_can_execute_a_command_and_cancel_it_immediately`
* **Purpose:** Validates that passing an already-canceled `CancellationToken` to `ExecuteBufferedAsync()` immediately aborts execution.
* **Setup:** Calls `cts.CancelAsync()` before command execution.
* **Arguments Passed:** `["sleep", "00:00:20"]`
* **Assertions:**
  * Throws an `OperationCanceledException`.
  * Exception's cancellation token matches `cts.Token`.

### 8. `I_can_execute_a_command_and_cancel_it_after_a_delay`
* **Purpose:** Validates that triggering cancellation while a command is running aborts the buffered execution task.
* **Setup:** `cts.CancelAfter(TimeSpan.FromSeconds(0.2))`
* **Arguments Passed:** `["sleep", "00:00:20"]`
* **Assertions:**
  * Throws an `OperationCanceledException`.
  * Exception's cancellation token matches `cts.Token`.

### 9. `I_can_execute_a_command_and_cancel_it_gracefully_after_a_delay`
* **Purpose:** Tests the overload of `ExecuteBufferedAsync` that accepts specific string encodings (`Encoding.Default`) and separate tokens for hard cancellation vs. graceful cancellation.
* **Setup:** Configures a graceful cancellation token to fire after `0.2` seconds while setting the standard cancellation token to `CancellationToken.None`.
* **Arguments Passed:** `["sleep", "00:00:20"]`
* **Method Call Signature:**
  ```csharp
  cmd.ExecuteBufferedAsync(
      Encoding.Default,
      Encoding.Default,
      CancellationToken.None, // Standard token
      cts.Token              // Graceful cancellation token
  );
  ```
* **Assertions:**
  * Throws an `OperationCanceledException`.
  * Exception's cancellation token matches `cts.Token`.