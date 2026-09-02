# CliWrap: `Command.Execution.cs` Technical Documentation Guide

## Overview

The `CliWrap/Command.Execution.cs` file is a core partial class implementation of `Command` in the CliWrap library. It handles the low-level execution lifecycle of external process instances. 

This file is responsible for:
- Resolving target executable paths (including platform-specific script resolution on Windows).
- Initializing and configuring `System.Diagnostics.Process` instances.
- Asynchronously managing standard input, standard output, and standard error streams.
- Implementing dual-stage cancellation handling (graceful and forceful).
- Enforcing resource usage policies (process priority, CPU affinity, working set limits).
- Validating process exit codes and constructing `CommandResult` responses.

---

## Key Components & Method Reference

### 1. Target Executable Path Resolution

#### `GetOptimallyQualifiedTargetFilePath()`
```csharp
internal string GetOptimallyQualifiedTargetFilePath()
```
* **Purpose**: Resolves the executable path, applying a workaround for Windows batch files (`.bat`, `.cmd`) when file extensions are omitted.
* **Logic & Behavior**:
  1. Checks if running on Windows using `OperatingSystem.IsWindows()`. If not on Windows, immediately returns `TargetFilePath`.
  2. If `TargetFilePath` is already fully qualified (`Path.IsPathFullyQualified`) or contains an extension (`Path.GetExtension`), returns `TargetFilePath` unchanged.
  3. Probes the following directories in order:
     - Directory of the current running process (`Environment.ProcessPath`).
     - Current working directory (`Directory.GetCurrentDirectory()`).
     - System `PATH` environment variable directories.
  4. For each existing probe directory, checks for the presence of the file appended with `.exe`, `.cmd`, or `.bat`.
  5. Returns the first matching file path found on disk; otherwise, falls back to `TargetFilePath`.

---

### 2. Stream Piping Engine

The execution engine uses dedicated private asynchronous methods to pipe process standard streams without causing deadlocks.

```
                  +-----------------------------------+
                  |      Process Execution Loop       |
                  +-----------------------------------+
                       /            |            \
                      /             |             \
                     v              v              v
     +-------------------+ +------------------+ +------------------+
     | PipeStandardInput | | PipeStandardOutput| | PipeStandardError|
     +-------------------+ +------------------+ +------------------+
               |                    |                    |
               v                    v                    v
     [ StandardInputPipe ]  [StandardOutputPipe] [ StandardErrorPipe ]
```

#### `PipeStandardInputAsync`
```csharp
private async Task PipeStandardInputAsync(Process process, CancellationToken cancellationToken = default)
```
* **Purpose**: Pipes data from `StandardInputPipe` to the process's standard input stream.
* **Key Details**:
  * Returns early if `process.StartInfo.RedirectStandardInput` is `false`.
  * Wraps `process.StandardInput.BaseStream` into an async-disposable wrapper.
  * Initiates `StandardInputPipe.CopyToAsync(...)` and applies a `.WaitAsync(cancellationToken)` fallback to handle non-responsive pipe sources.
  * Catches `OperationCanceledException` and explicitly observes background exceptions via `.Catch()` to prevent unhandled exception propagation on the task scheduler.
  * Catches exact `IOException` instances (e.g., "The pipe has been ended" or "Broken pipe"). These occur naturally if the process exits before reading all available standard input data and are not treated as exceptional errors.

#### `PipeStandardOutputAsync`
```csharp
private async Task PipeStandardOutputAsync(Process process, CancellationToken cancellationToken = default)
```
* **Purpose**: Pipes data from the process's standard output stream to `StandardOutputPipe`.
* **Key Details**:
  * Returns early if `process.StartInfo.RedirectStandardOutput` is `false`.
  * Invokes `StandardOutputPipe.CopyFromAsync(...)` on `process.StandardOutput.BaseStream`.

#### `PipeStandardErrorAsync`
```csharp
private async Task PipeStandardErrorAsync(Process process, CancellationToken cancellationToken = default)
```
* **Purpose**: Pipes data from the process's standard error stream to `StandardErrorPipe`.
* **Key Details**:
  * Returns early if `process.StartInfo.RedirectStandardError` is `false`.
  * Invokes `StandardErrorPipe.CopyFromAsync(...)` on `process.StandardError.BaseStream`.

---

### 3. Core Process Execution Loop

#### `ExecuteAsync` (Internal Process Monitor)
```csharp
private async Task<int> ExecuteAsync(
    Process process,
    CancellationToken forcefulCancellationToken = default,
    CancellationToken gracefulCancellationToken = default)
```
* **Purpose**: Manages the runtime lifecycle of an active process, background stream tasks, signal handling, exit code validation, and cleanup.
* **Execution Flow**:
  1. **Disposal Management**: Wraps `process` in a `using` statement to guarantee resource disposal.
  2. **Linked Cancellation Tokens**:
     * `forcefulCancellationOrPanicCts`: Linked to `forcefulCancellationToken`. Ensures process termination on failure or user cancellation request.
     * `forcefulCancellationOrPanicOrExitCts`: Linked to `forcefulCancellationOrPanicCts.Token`. Triggers standard input pipe cancellation as soon as the process exits.
  3. **Signal Handlers**:
     * Registers `process.TryKill()` against `forcefulCancellationOrPanicCts.Token`.
     * Registers `process.TryInterrupt()` against `gracefulCancellationToken`.
  4. **Task Orchestration**:
     * Launches `stdInTask`, `stdOutTask`, and `stdErrTask`.
     * Launches `processTask` using `process.WaitForExitAsync(CancellationToken.None)`.
  5. **Task Loop Monitoring (`Task.WhenEach`)**:
     * If `processTask` completes first, triggers `forcefulCancellationOrPanicOrExitCts.CancelAsync()` to shut down standard input piping.
     * If a piping task fails while the process is still running, triggers `forcefulCancellationOrPanicCts.CancelAsync()` to kill the process immediately and avoid unnecessary waiting.
  6. **Task Join & Exception Propagation**:
     * Awaits `Task.WhenAll(...)` across all tasks to aggregate exceptions. Suppresses internal cancellation exceptions caused by pipe aborts or process exits.
  7. **Cancellation Reporting**:
     * Throws an `OperationCanceledException` if `forcefulCancellationToken` or `gracefulCancellationToken` was requested.
  8. **Exit Code Validation**:
     * Checks if `process.ExitCode != 0` and `Validation.HasFlag(CommandResultValidation.ZeroExitCode)`.
     * Throws a `CommandExecutionException` containing execution details if validation fails.
  9. **Return**: Returns the integer `ExitCode`.

---

### 4. Process Creation & Configuration Overloads

#### `ExecuteAsync` (Process Setup & Start)
```csharp
private CommandTask<CommandResult> ExecuteAsync(
    ProcessStartInfo processStartInfo,
    Action<Process>? configureProcess = null,
    CancellationToken forcefulCancellationToken = default,
    CancellationToken gracefulCancellationToken = default)
```
* **Purpose**: Instantiates `Process`, starts execution synchronously, applies resource policies, and hooks up the task wrapper.
* **Logic**:
  1. Instantiates `Process` using `ProcessStartInfo`.
  2. Executes `process.Start()`. Throws `InvalidOperationException` if process creation returns `false`.
  3. Catches underlying system errors and wraps them in a `Win32Exception` providing context on binary path / directory validity.
  4. Records `startTime = DateTimeOffset.Now` and captures `process.Id`.
  5. **Applies Resource Policy**:
     * Assigns `Priority` (`process.PriorityClass`), `Affinity` (`process.ProcessorAffinity`), `MinWorkingSet`, and `MaxWorkingSet`.
     * Catches `NotSupportedException` (if platform unsupported) and ignores `InvalidOperationException` (if the process exited before policies could be applied).
  6. Executes the optional user configuration delegate: `configureProcess?.Invoke(process)`.
  7. Converts the execution task into a `CommandTask<CommandResult>` containing the exit code, start time, and end time.
  8. If any synchronous startup steps throw, disposes the process before rethrowing.

#### `ExecuteAsync` (Public Process Configuration Overload)
```csharp
public CommandTask<CommandResult> ExecuteAsync(
    Action<ProcessStartInfo>? configureProcessStartInfo,
    Action<Process>? configureProcess = null,
    CancellationToken forcefulCancellationToken = default,
    CancellationToken gracefulCancellationToken = default)
```
* **Purpose**: Allows direct, low-level customization of `ProcessStartInfo` and `Process` objects before execution.
* **Default Settings Assigned**:
  * `FileName`: Resolved path via `GetOptimallyQualifiedTargetFilePath()`.
  * `Arguments`: Command arguments string.
  * `WorkingDirectory`: Command target working directory path.
  * Stream redirection enabled (`RedirectStandardInput`, `RedirectStandardOutput`, `RedirectStandardError` set to `true`).
  * `UseShellExecute = false`.
  * `CreateNoWindow = true` (Prevents console child processes from attaching to the parent console window on Windows).
* **Credential Assignment**:
  * Configures `Domain`, `UserName`, `Password` (converted via `.ToSecureString()`), and `LoadUserProfile`.
  * Catches and rethrows platform-unsupported credential options as `NotSupportedException`.
* **Environment Variable Resolution**:
  * Iterates over `EnvironmentVariables`. Sets defined values; removes environment key if `value` is `null`.
* **Execution**: Invokes user modification delegate `configureProcessStartInfo?.Invoke(processStartInfo)` and delegates to the private overload.

---

### 5. Standard Public API Overloads

```csharp
public CommandTask<CommandResult> ExecuteAsync(
    CancellationToken forcefulCancellationToken,
    CancellationToken gracefulCancellationToken)
```
* **Purpose**: Main cancellation-aware overload.
* **Behavior**: Forwards calls to the configuration overload with `null` delegates.

```csharp
public CommandTask<CommandResult> ExecuteAsync(CancellationToken cancellationToken = default)
```
* **Purpose**: Primary convenience overload.
* **Behavior**: Maps `cancellationToken` to `forcefulCancellationToken` and uses `CancellationToken.None` for `gracefulCancellationToken`.

---

## Execution & Lifecycle Flow

```
+-------------------------------------------------------------------+
| 1. ExecuteAsync(forcefulToken, gracefulToken)                     |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
| 2. Build ProcessStartInfo                                         |
|    - Resolve optimally qualified target path (Windows workaround) |
|    - Populate credentials & environment variables                 |
|    - Apply user-defined ProcessStartInfo modifications            |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
| 3. Process Initialization & Resource Policy Setup                 |
|    - Call process.Start()                                         |
|    - Record Start Time & Process ID                               |
|    - Set Priority, Affinity, and Working Set limits               |
|    - Apply user-defined Process modifications                     |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
| 4. Execute Async Piping Loop & Monitoring                         |
|    - Register process.TryKill() & process.TryInterrupt() signals  |
|    - Start StdIn, StdOut, StdErr background piping tasks          |
|    - Start process.WaitForExitAsync()                             |
|    - Monitor task execution using Task.WhenEach                   |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
| 5. Process Completion & Validation                                |
|    - Join tasks using Task.WhenAll                                |
|    - Check for cancellation requests                              |
|    - Validate exit code (Throw CommandExecutionException if non-0)|
|    - Return CommandResult(ExitCode, StartTime, EndTime)           |
+-------------------------------------------------------------------+
```

---

## Exception Reference

| Exception Type | Trigger Condition |
| :--- | :--- |
| `InvalidOperationException` | Thrown if `process.Start()` returns `false` during startup. |
| `Win32Exception` | Thrown if binary start fails due to native OS limitations (missing binary, invalid permissions, missing working directory). |
| `NotSupportedException` | Thrown if user attempts to set process credentials or resource policies on unsupported platforms. |
| `OperationCanceledException` | Thrown if either `forcefulCancellationToken` or `gracefulCancellationToken` is triggered during command execution. |
| `CommandExecutionException` | Thrown after process execution completes if the exit code is non-zero and `CommandResultValidation.ZeroExitCode` validation is enabled. |