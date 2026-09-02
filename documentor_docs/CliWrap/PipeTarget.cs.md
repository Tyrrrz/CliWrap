# Documentation: `CliWrap/PipeTarget.cs`

## Overview

The `PipeTarget` abstract class in the `CliWrap` namespace defines the contract and factory methods for redirecting a process's standard output (`stdout`) or standard error (`stderr`) streams. It acts as a sink for binary data emitted by execution processes, providing built-in abstractions for piping output to streams, files, string builders, delegates, or multiple targets simultaneously.

---

## Class Architecture

`PipeTarget` is defined across partial class declarations and includes a base abstract contract along with private nested concrete implementations.

```
PipeTarget (abstract)
 ├── AnonymousPipeTarget (private nested)
 └── AggregatePipeTarget (private nested)
```

### Base Class Signature

```csharp
public abstract partial class PipeTarget
```

---

## Core Method

### `CopyFromAsync`

```csharp
public abstract Task CopyFromAsync(
    Stream origin,
    CancellationToken cancellationToken = default
);
```

* **Purpose**: Reads binary data from the `origin` stream (representing the process's standard output or standard error stream) and pushes it into the pipe target destination.
* **Parameters**:
  * `origin`: The source stream containing data emitted by the underlying process.
  * `cancellationToken`: Token used to observe cancellation requests.
* **Returns**: A `Task` that completes when all data from the origin stream has been processed by the target.

---

## Nested Classes

### 1. `AnonymousPipeTarget`

```csharp
private class AnonymousPipeTarget(Func<Stream, CancellationToken, Task> copyFromAsync) : PipeTarget
```

* **Description**: Wraps a custom asynchronous delegate `Func<Stream, CancellationToken, Task>` to create a lightweight implementation of `PipeTarget`.
* **Behavior**: Delegates `CopyFromAsync` calls directly to the underlying `copyFromAsync` function while configuring awaiters to not capture the synchronization context (`ConfigureAwait(false)`).

### 2. `AggregatePipeTarget`

```csharp
private class AggregatePipeTarget(IReadOnlyList<PipeTarget> targets) : PipeTarget
```

* **Description**: Replicates incoming stream data to a collection of multiple inner `PipeTarget` instances in parallel.
* **Properties**:
  * `Targets`: An `IReadOnlyList<PipeTarget>` containing the inner pipe targets.
* **Execution Flow**:
  1. Creates a linked `CancellationTokenSource` (`cancellationOrPanicCts`) from the provided cancellation token.
  2. Spawns an in-memory `SimplexStream` instance for each target.
  3. Launches background tasks (`Task.WhenAll`) to feed each target from its corresponding `SimplexStream`. If any child target throws an exception, `cancellationOrPanicCts.CancelAsync()` is triggered to abort all targets.
  4. Rents a byte buffer from `MemoryPool<byte>.Shared` using size `BufferSizes.Stream`.
  5. Continually reads from the `origin` stream and writes the read bytes to every target's `SimplexStream`.
  6. Signals completion to all `SimplexStream` instances via `CloseAsync()`.
  7. Awaits the background reading task to propagate any errors or exceptions.
  8. Asynchronously disposes all created `SimplexStream` instances in a `finally` block.

---

## Factory Properties & Methods

### Null Target

#### `PipeTarget.Null`
```csharp
public static PipeTarget Null { get; }
```
* **Description**: Represents a pipe target that discards all data.
* **Behavior**: Returns `Task.CompletedTask` (or `Task.FromCanceled` if already requested). Using `Null` signals to the process execution context that the standard stream should not be opened at all, eliminating unnecessary consumption and memory overhead.

---

### Custom Target Creation

#### `Create(Func<Stream, CancellationToken, Task>)`
```csharp
public static PipeTarget Create(Func<Stream, CancellationToken, Task> copyFromAsync)
```
* **Description**: Instantiates an `AnonymousPipeTarget` with an asynchronous delegate.

#### `Create(Action<Stream>)`
```csharp
public static PipeTarget Create(Action<Stream> copyFrom)
```
* **Description**: Wraps a synchronous delegate that accepts the origin stream into a completed task execution.

---

### Stream Piping

#### `ToStream(Stream stream, bool autoFlush)`
```csharp
public static PipeTarget ToStream(Stream stream, bool autoFlush)
```
* **Description**: Pipes the binary data to a destination `Stream`.
* **Behavior**: Calls `origin.CopyToAsync(stream, autoFlush, cancellationToken)`.

#### `ToStream(Stream stream)`
```csharp
public static PipeTarget ToStream(Stream stream)
```
* **Description**: Overload that defaults `autoFlush` to `true`.

---

### File Piping

#### `ToFile(string filePath)`
```csharp
public static PipeTarget ToFile(string filePath)
```
* **Description**: Directs output to a file specified by `filePath`.
* **File Options**:
  * Mode: `FileMode.Create`
  * Access: `FileAccess.Write`
  * Share: `FileShare.Read`
  * Buffer Size: `BufferSizes.Stream`
  * Options: `FileOptions.Asynchronous`
* **Behavior**: Wraps the created `FileStream` with `file.ToAsyncDisposable()` and asynchronously copies data from `origin` into the file.

---

### String Builder Piping

#### `ToStringBuilder(StringBuilder stringBuilder, Encoding encoding)`
```csharp
public static PipeTarget ToStringBuilder(StringBuilder stringBuilder, Encoding encoding)
```
* **Description**: Decodes output from the stream using the specified `Encoding` and appends it to a `StringBuilder`.
* **Implementation**:
  * Uses a `StreamReader` configured with `BufferSizes.StreamReader` and `leaveOpen: true`.
  * Rents character buffers from `MemoryPool<char>.Shared.Rent(BufferSizes.StreamReader)`.
  * Reads asynchronously in chunks and appends the read slice (`buffer.Memory[..charsRead]`) to the `stringBuilder`.

#### `ToStringBuilder(StringBuilder stringBuilder)`
```csharp
public static PipeTarget ToStringBuilder(StringBuilder stringBuilder)
```
* **Description**: Overload defaulting to `Encoding.Default`.

---

### Delegate / Line Piping

#### `ToDelegate(Func<string, CancellationToken, Task> handleLineAsync, Encoding encoding)`
```csharp
public static PipeTarget ToDelegate(Func<string, CancellationToken, Task> handleLineAsync, Encoding encoding)
```
* **Description**: Reads the stream line-by-line using `StreamReader.ReadLinesAsync()` and executes an asynchronous delegate for every line.

#### `ToDelegate(Func<string, CancellationToken, Task> handleLineAsync)`
```csharp
public static PipeTarget ToDelegate(Func<string, CancellationToken, Task> handleLineAsync)
```
* **Description**: Overload using `Encoding.Default`.

#### `ToDelegate(Func<string, Task> handleLineAsync, Encoding encoding)`
```csharp
public static PipeTarget ToDelegate(Func<string, Task> handleLineAsync, Encoding encoding)
```
* **Description**: Overload for asynchronous delegates that do not accept a `CancellationToken`.

#### `ToDelegate(Func<string, Task> handleLineAsync)`
```csharp
public static PipeTarget ToDelegate(Func<string, Task> handleLineAsync)
```
* **Description**: Overload using `Encoding.Default` for line tasks without cancellation tokens.

#### `ToDelegate(Action<string> handleLine, Encoding encoding)`
```csharp
public static PipeTarget ToDelegate(Action<string> handleLine, Encoding encoding)
```
* **Description**: Overload for synchronous delegates operating on each line.

#### `ToDelegate(Action<string> handleLine)`
```csharp
public static PipeTarget ToDelegate(Action<string> handleLine)
```
* **Description**: Overload using `Encoding.Default` for synchronous line handlers.

---

### Target Merging / Aggregation

#### `Merge(IEnumerable<PipeTarget> targets)`
```csharp
public static PipeTarget Merge(params IEnumerable<PipeTarget> targets)
```
* **Description**: Merges multiple `PipeTarget` instances into a single aggregated target that replicates output to all child targets.
* **Optimization Steps**:
  1. **Unwrapping**: Recursively flattens nested `AggregatePipeTarget` instances via `FlattenTargets` into a single flat list.
  2. **Filtering**: Removes all references to `PipeTarget.Null`.
  3. **Evaluation**:
     * If the resulting list contains **1 target**: Returns that single target directly.
     * If the resulting list contains **0 targets**: Returns `PipeTarget.Null`.
     * If 2 or more targets remain: Instantiates and returns an `AggregatePipeTarget`.

#### `Merge(params PipeTarget[] targets)`
```csharp
public static PipeTarget Merge(params PipeTarget[] targets)
```
* **Description**: Array-based params overload forwarding to `Merge((IEnumerable<PipeTarget>)targets)`.

---

## Memory & Performance Features

1. **Shared Memory Pools**: Buffer allocations for binary reads (`AggregatePipeTarget`) and character reads (`ToStringBuilder`) rent memory from `MemoryPool<byte>.Shared` and `MemoryPool<char>.Shared` respectively, reducing GC pressure.
2. **Buffer Sizes**: Uses centralized buffer sizes (`BufferSizes.Stream` and `BufferSizes.StreamReader`) defined in `CliWrap.Utils`.
3. **No-Capture Async Execution**: All asynchronous calls explicitly specify `.ConfigureAwait(false)` to minimize thread context switching overhead.
4. **Stream Duplication via SimplexStream**: `AggregatePipeTarget` utilizes `SimplexStream` objects to decouple slow targets from fast targets through concurrent background workers.