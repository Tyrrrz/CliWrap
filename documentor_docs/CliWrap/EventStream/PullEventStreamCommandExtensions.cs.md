# Technical Documentation: `CliWrap/EventStream/PullEventStreamCommandExtensions.cs`

## Overview

The `PullEventStreamCommandExtensions.cs` file provides an event streaming execution model for `CliWrap.Command` instances using C#'s `IAsyncEnumerable<CommandEvent>`. It implements a **pull-based** event stream model where command events—such as process start, standard output lines, standard error lines, and process exit—are yielded asynchronously as the caller iterates over the stream.

---

## File Identification

* **File Path:** `CliWrap/EventStream/PullEventStreamCommandExtensions.cs`
* **Namespace:** `CliWrap.EventStream`
* **Class Definition:** `public static partial class EventStreamCommandExtensions`

---

## Core Architecture & Execution Model

The pull-based model converts command execution into an asynchronous enumerable sequence of `CommandEvent` objects. 

### Key Components

1. **`Channel<CommandEvent>`**: An in-memory channel (via `CliWrap.Utils`) used as a buffer to transfer line events captured from redirected standard output and error streams to the consumer iterating over `IAsyncEnumerable<CommandEvent>`.
2. **Pipe Merging (`PipeTarget.Merge`)**: Standard output and standard error targets are merged with delegate targets (`PipeTarget.ToDelegate`). As lines are read from stdout/stderr using the specified `Encoding`, delegate callbacks transmit `StandardOutputCommandEvent` or `StandardErrorCommandEvent` instances into the channel.
3. **Linked Cancellation Token Source**: A `CancellationTokenSource` linked to the consumer's `forcefulCancellationToken` is created (`forcefulCancellationOrDisposeCts`). This ensures the process can be forcefully cancelled either explicitly via token cancellation or implicitly when the consumer abandons the iterator.
4. **Task Wrapper (`Wrap`)**: Extends the execution task to guarantee channel closure upon command completion or failure, while properly translating internal cancellation exceptions back to consumer-provided cancellation tokens.

---

## Event Sequence Flow

When iterating over `ListenAsync`, events are yielded in the following deterministic sequence:

1. **`StartedCommandEvent`**: Yielded immediately after launching the underlying process. Contains the `ProcessId`.
2. **`StandardOutputCommandEvent` / `StandardErrorCommandEvent`**: Yielded as the process outputs text to standard output or standard error.
3. **`ExitedCommandEvent`**: Yielded once the process finishes executing. Contains the process's `ExitCode`.

---

## Method Documentation

### Primary Execution Method

```csharp
public async IAsyncEnumerable<CommandEvent> ListenAsync(
    Encoding standardOutputEncoding,
    Encoding standardErrorEncoding,
    [EnumeratorCancellation] CancellationToken forcefulCancellationToken,
    CancellationToken gracefulCancellationToken
)
```

Executes the command and yields events via an `IAsyncEnumerable<CommandEvent>`.

#### Parameters

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `standardOutputEncoding` | `Encoding` | Encoding used to parse standard output lines. |
| `standardErrorEncoding` | `Encoding` | Encoding used to parse standard error lines. |
| `forcefulCancellationToken` | `CancellationToken` | Token used to trigger immediate, forceful cancellation of the command process. Marked with `[EnumeratorCancellation]`. |
| `gracefulCancellationToken` | `CancellationToken` | Token used to request graceful cancellation (e.g., sending SIGINT). |

#### Returns

* `IAsyncEnumerable<CommandEvent>`: An async-enumerable sequence yielding command execution events as they occur.

#### Iterator Abandonment & Cleanup Mechanics

If the consuming code stops iterating prematurely (e.g., via `break`, `return`, or an unhandled exception inside the `await foreach` loop):
* The `finally` block of the iterator executes `await forcefulCancellationOrDisposeCts.CancelAsync()`.
* Forceful cancellation terminates the underlying process to prevent hanging process leaks when output pipes are no longer being drained.
* Internal cancellation exceptions resulting strictly from iterator abandonment are caught and suppressed in the `finally` block.

---

### Convenience Overloads

The class provides three convenience overloads that delegate to the primary `ListenAsync` method:

#### 1. Overload with Explicit Encodings and Default Graceful Cancellation Token

```csharp
public IAsyncEnumerable<CommandEvent> ListenAsync(
    Encoding standardOutputEncoding,
    Encoding standardErrorEncoding,
    CancellationToken cancellationToken = default
)
```
* **Behavior:** Forwards calls to the primary method using `cancellationToken` as `forcefulCancellationToken` and `CancellationToken.None` as `gracefulCancellationToken`.

#### 2. Overload with Single Shared Encoding

```csharp
public IAsyncEnumerable<CommandEvent> ListenAsync(
    Encoding encoding,
    CancellationToken cancellationToken = default
)
```
* **Behavior:** Applies the provided `encoding` to both standard output and standard error pipes.

#### 3. Default Overload

```csharp
public IAsyncEnumerable<CommandEvent> ListenAsync(
    CancellationToken cancellationToken = default
)
```
* **Behavior:** Uses `Encoding.Default` for both standard output and standard error streams.

---

## Error & Exception Handling Details

* **`OperationCanceledException` Translation:** If execution is cancelled via `forcefulCancellationOrDisposeCts` due to `forcefulCancellationToken` being requested, the internal wrapper rethrows an `OperationCanceledException` tied directly to `forcefulCancellationToken`.
* **Channel Race Condition Protection:** When closing the event channel in the command completion wrapper (`channel.CloseAsync(...)`), any `OperationCanceledException` triggered by concurrent cancellation is caught and swallowed to prevent race-condition exceptions.