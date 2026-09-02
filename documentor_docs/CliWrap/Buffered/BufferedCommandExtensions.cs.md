# Technical Documentation: `BufferedCommandExtensions.cs`

## Overview

The `BufferedCommandExtensions` class is part of the **CliWrap.Buffered** namespace. It provides extension methods for the `Command` class to enable **buffered execution**. 

In standard process execution, output streams (`stdout` and `stderr`) are processed as raw byte streams. The buffered execution model captures and decodes standard output and standard error data into text strings, returning them alongside execution metadata inside a `BufferedCommandResult` object.

---

## File Details

* **File Path:** `CliWrap/Buffered/BufferedCommandExtensions.cs`
* **Namespace:** `CliWrap.Buffered`
* **Target Type:** `CliWrap.Command`
* **Class Type:** `public static class`

---

## Key Components

### Dependencies

* `System.Text`: Used for text `Encoding` operations and `StringBuilder`.
* `System.Threading`: Used for handling `CancellationToken` instances.
* `CliWrap.Exceptions`: Provides the `CommandExecutionException` type for handling command failures.

---

## Detailed Extension Methods

All methods in this class extend `Command` and return a `CommandTask<BufferedCommandResult>`.

### 1. Fully-Configured Overload (Core Logic)

```csharp
public CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
    Encoding standardOutputEncoding,
    Encoding standardErrorEncoding,
    CancellationToken forcefulCancellationToken,
    CancellationToken gracefulCancellationToken
)
```

#### Purpose
The primary execution method that configures standard output/error buffering, executes the command asynchronously, wraps the execution task, and handles execution errors.

#### Parameters
* `standardOutputEncoding` (`Encoding`): Encoding used to decode bytes from `stdout`.
* `standardErrorEncoding` (`Encoding`): Encoding used to decode bytes from `stderr`.
* `forcefulCancellationToken` (`CancellationToken`): Token to abruptly cancel execution.
* `gracefulCancellationToken` (`CancellationToken`): Token to request a graceful shutdown.

#### Execution Workflow
1. **Buffer Instantiation**: Creates two `StringBuilder` instances (`stdOutBuffer` and `stdErrBuffer`).
2. **Pipe Extension**:
   * Extends the existing `command.StandardOutputPipe` using `PipeTarget.Merge` to simultaneously send output to both its original destination and a `ToStringBuilder` target configured with `stdOutBuffer` and `standardOutputEncoding`.
   * Extends `command.StandardErrorPipe` similarly using `PipeTarget.Merge` with `stdErrBuffer` and `standardErrorEncoding`.
3. **Command Execution**: Calls `ExecuteAsync` on the modified command passing the forcefully and gracefully provided cancellation tokens.
4. **Task Result Transformation**:
   * Uses `.Wrap()` on `CommandTask` to intercept the returned result.
   * **Success Path**: Constructs and returns a `BufferedCommandResult` passing:
     * `result.ExitCode`
     * `result.StartTime`
     * `result.ExitTime`
     * `stdOutBuffer.ToString()`
     * `stdErrBuffer.ToString()`
   * **Failure Path (`CommandExecutionException`)**:
     * Catches `CommandExecutionException`.
     * Re-throws a new `CommandExecutionException` containing the command instance, exit code, inner exception, and a modified error message detailing the trimmed standard error string (`stdErrBuffer.ToString().Trim()`).

---

### 2. Dual-Encoding Overload with Single Cancellation Token

```csharp
public CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
    Encoding standardOutputEncoding,
    Encoding standardErrorEncoding,
    CancellationToken cancellationToken = default
)
```

#### Purpose
A convenience overload that accepts separate encodings for `stdout` and `stderr` and a single `cancellationToken`.

#### Delegation
Delegates to the primary `ExecuteBufferedAsync` overload, passing `cancellationToken` as the `forcefulCancellationToken` and `CancellationToken.None` as the `gracefulCancellationToken`.

---

### 3. Unified-Encoding Overload

```csharp
public CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
    Encoding encoding,
    CancellationToken cancellationToken = default
)
```

#### Purpose
A convenience overload that applies the same text encoding to both standard output and standard error.

#### Delegation
Delegates to the dual-encoding overload, passing `encoding` for both `standardOutputEncoding` and `standardErrorEncoding`.

---

### 4. Default Parameterless Overload

```csharp
public CommandTask<BufferedCommandResult> ExecuteBufferedAsync(
    CancellationToken cancellationToken = default
)
```

#### Purpose
The simplest overload for executing a command with standard buffering using default system encoding.

#### Delegation
Delegates to the unified-encoding overload using `Encoding.Default`.

---

## Error Handling Mechanics

When a command exits with a non-zero exit code, CliWrap throws a `CommandExecutionException`. 

The buffered extension intercepts this exception during task wrapping and enriches it by appending the captured standard error buffer (`stdErrBuffer`) to the exception message:

```text
Command execution failed, see the inner exception for details.

Standard error:
<captured standard error text>
```

This improves diagnostic capabilities by including the process output directly in the failure message.