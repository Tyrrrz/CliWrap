# Documentation: `CliWrap/EventStream/PushEventStreamCommandExtensions.cs`

## Overview

The `PushEventStreamCommandExtensions.cs` file provides extension methods for executing CLI commands using a **push-based event stream model**. It exposes the command execution lifecycle as an `IObservable<CommandEvent>`, allowing reactive consumers to observe process execution events such as standard output, standard error, process start, and process exit in real time.

---

## Class Architecture

- **Namespace:** `CliWrap.EventStream`
- **Class:** `public static partial class EventStreamCommandExtensions`
- **Target Type:** `Command`

This partial static class extends `CliWrap.Command` objects to add support for reactive observation (`IObservable<CommandEvent>`).

---

## Key Components & Methods

### 1. Primary Extension Method: `Observe`

```csharp
public IObservable<CommandEvent> Observe(
    Encoding standardOutputEncoding,
    Encoding standardErrorEncoding,
    CancellationToken forcefulCancellationToken,
    CancellationToken gracefulCancellationToken
)
```

Executes the command as an observable event stream using specified encodings and cancellation tokens.

#### **Parameters**
- `standardOutputEncoding`: Encoding used to parse lines from stdout.
- `standardErrorEncoding`: Encoding used to parse lines from stderr.
- `forcefulCancellationToken`: Cancellation token to trigger immediate/forceful termination of the underlying process.
- `gracefulCancellationToken`: Cancellation token to trigger graceful shutdown of the process.

#### **Return Value**
- `IObservable<CommandEvent>`: A synchronized observable stream emitting command execution events.

---

### 2. Convenience Overloads

The primary method is accompanied by three convenience overloads:

1. **Custom stdout/stderr encodings with single cancellation token:**
   ```csharp
   public IObservable<CommandEvent> Observe(
       Encoding standardOutputEncoding,
       Encoding standardErrorEncoding,
       CancellationToken cancellationToken = default
   )
   ```
   *Passes `cancellationToken` as `forcefulCancellationToken` and `CancellationToken.None` as `gracefulCancellationToken`.*

2. **Single encoding for both stdout/stderr:**
   ```csharp
   public IObservable<CommandEvent> Observe(
       Encoding encoding,
       CancellationToken cancellationToken = default
   )
   ```
   *Uses `encoding` for both standard output and standard error.*

3. **Default encoding (`Encoding.Default`):**
   ```csharp
   public IObservable<CommandEvent> Observe(
       CancellationToken cancellationToken = default
   )
   ```
   *Uses `Encoding.Default` for standard output and standard error.*

---

## How It Works

### Execution & Event Lifecycle Flow

```
+-----------------------------------------------------------------------+
|                         Observable Subscription                       |
+-----------------------------------------------------------------------+
                                    |
                                    v
            +-----------------------------------------------+
            |  Create Linked Token Source                   |
            |  (forcefulCancellationToken + Unsubscribe)    |
            +-----------------------------------------------+
                                    |
                                    v
            +-----------------------------------------------+
            |  Merge Standard Output & Error Pipes          |
            |  Pushes stdout/stderr lines to observer       |
            +-----------------------------------------------+
                                    |
                                    v
            +-----------------------------------------------+
            |  ExecuteAsync(...)                            |
            +-----------------------------------------------+
                                    |
                                    v
            +-----------------------------------------------+
            |  Emit StartedCommandEvent(ProcessId)          |
            +-----------------------------------------------+
                                    |
                                    v
            +-----------------------------------------------+
            |  Await Execution Task completion              |
            +-----------------------------------------------+
                                    |
              +---------------------+---------------------+
              |                                           |
      [ Success ]                                    [ Exception ]
              |                                           |
              v                                           v
+-----------------------------+             +-----------------------------+
| Emit ExitedCommandEvent     |             | Translate Linked Token      |
| Call observer.OnCompleted() |             | Call observer.OnError(...)  |
+-----------------------------+             | Rethrow Exception           |
                                            +-----------------------------+
```

### Detailed Mechanics

1. **Synchronized Observable Creation:**
   The stream is instantiated via `Observable.CreateSynchronized<CommandEvent>`, ensuring thread-safe dispatches to `observer.OnNext`.

2. **Linked Cancellation Token:**
   Creates a linked token source (`forcefulCancellationOrUnsubscribeCts`) merging the provided `forcefulCancellationToken` with the observable subscription lifetime. This ensures that unsubscribing from the observable automatically cancels process execution.

3. **Pipe Augmentation:**
   - Standard output is intercepted using `PipeTarget.Merge`, combining existing pipes with a delegate target emitting `StandardOutputCommandEvent(line)`.
   - Standard error is intercepted similarly, emitting `StandardErrorCommandEvent(line)`.

4. **Task Execution & Wrapping:**
   - Starts command execution via `ExecuteAsync(...)`.
   - The execution task is wrapped using `.Wrap(...)`:
     - Emits `StartedCommandEvent(task.ProcessId)` immediately.
     - Awaits process completion asynchronously (`await task.ConfigureAwait(false)`).
     - Upon completion, emits `ExitedCommandEvent(result.ExitCode)` followed by `observer.OnCompleted()`.

5. **Error & Cancellation Handling:**
   - If an `OperationCanceledException` occurs due to the linked cancellation token, the exception is translated back to reference the consumer's `forcefulCancellationToken` before invoking `observer.OnError(translatedEx)`.
   - Any general exceptions invoke `observer.OnError(ex)` and are rethrown.

6. **Cleanup Mechanics (Unsubscribing / Disposal):**
   When the consumer unsubscribes or disposes of the observable subscription:
   - The linked token source (`forcefulCancellationOrUnsubscribeCts`) is cancelled and disposed, terminating the process to prevent orphaned processes or hanging streams.
   - The execution task is detached in the background using `_ = commandTask.Task.Catch();` to avoid blocking synchronous threads and preventing potential deadlocks.

---

## Summary of Emitted Events

| Event Type | Trigger | Content |
| :--- | :--- | :--- |
| `StartedCommandEvent` | Process execution starts | Process ID (`task.ProcessId`) |
| `StandardOutputCommandEvent` | Standard output line received | Output line text |
| `StandardErrorCommandEvent` | Standard error line received | Error line text |
| `ExitedCommandEvent` | Process finishes execution | Exit code (`result.ExitCode`) |