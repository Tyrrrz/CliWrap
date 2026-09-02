# Technical Documentation: `CliWrap.PipeSource`

The `PipeSource` class in `CliWrap` represents a data source that feeds standard input (`stdin`) to a child process. It acts as an abstraction for writing binary or text data, stream contents, file data, or outputs from other commands into the input stream of an executing process.

---

## Architecture Overview

`PipeSource` is an `abstract partial class` defined in the `CliWrap` namespace. It exposes static factory methods to construct various input piping sources and relies on an internal subclass `AnonymousPipeSource` to wrap inline delegates.

```
                  ┌──────────────────┐
                  │    PipeSource    │  (Abstract Base)
                  └────────┬─────────┘
                           │
                           │ Implements CopyToAsync
                           ▼
             ┌───────────────────────────┐
             │    AnonymousPipeSource    │  (Private Nested Implementation)
             └───────────────────────────┘
```

---

## Core Abstract Method

### `CopyToAsync`

```csharp
public abstract Task CopyToAsync(
    Stream destination,
    CancellationToken cancellationToken = default
);
```

- **Description**: Reads binary content from the source and writes it asynchronously into `destination`. In the context of CliWrap, `destination` represents the child process's standard input stream.
- **Parameters**:
  - `destination`: The standard input target stream of the process.
  - `cancellationToken`: A token to monitor for cancellation requests.
- **Returns**: A `Task` representing the asynchronous copy operation.

---

## Nested Classes

### `AnonymousPipeSource`

```csharp
private class AnonymousPipeSource(Func<Stream, CancellationToken, Task> copyToAsync) : PipeSource
```

A private implementation of `PipeSource` that delegates the execution of `CopyToAsync` to an underlying `Func<Stream, CancellationToken, Task>` delegate. It invokes `.ConfigureAwait(false)` on the execution task.

---

## Static Properties & Factory Methods

### 1. `Null` Property

```csharp
public static PipeSource Null { get; }
```

- **Description**: Represents an empty pipe source that provides no data. It is functionally equivalent to reading from a null device (e.g., `/dev/null` or `NUL`).
- **Behavior**: Returns `Task.CompletedTask` if cancellation is not requested, or `Task.FromCanceled(cancellationToken)` if cancellation is requested prior to execution.

---

### 2. Custom Delegate Creation

#### `Create(Func<Stream, CancellationToken, Task> copyToAsync)`
Creates an anonymous `PipeSource` where `CopyToAsync` is driven by a custom asynchronous delegate.

```csharp
public static PipeSource Create(Func<Stream, CancellationToken, Task> copyToAsync)
```

#### `Create(Action<Stream> copyTo)`
Creates an anonymous `PipeSource` driven by a custom synchronous action. Converts the synchronous invocation into a `Task.CompletedTask`.

```csharp
public static PipeSource Create(Action<Stream> copyTo)
```

---

### 3. Stream-Based Sources

#### `FromStream(Stream stream, bool autoFlush)`
Creates a `PipeSource` that reads binary data from an existing `Stream`.

```csharp
public static PipeSource FromStream(Stream stream, bool autoFlush)
```
- Uses the `stream.CopyToAsync(destination, autoFlush, cancellationToken)` extension method.

#### `FromStream(Stream stream)`
Overload that defaults `autoFlush` to `true`.

```csharp
public static PipeSource FromStream(Stream stream)
```

---

### 4. File-Based Sources

#### `FromFile(string filePath)`

```csharp
public static PipeSource FromFile(string filePath)
```

- **Description**: Reads data from a file on disk at `filePath` and pushes it into the pipe.
- **Implementation Details**:
  - Constructs a `FileStream` configured with:
    - `FileMode.Open`
    - `FileAccess.Read`
    - `FileShare.Read`
    - Buffer size defined by `BufferSizes.Stream`
    - Options: `FileOptions.Asynchronous | FileOptions.SequentialScan`
  - Manages asynchronous disposal of the file stream via `file.ToAsyncDisposable()`.
  - Asynchronously copies content to the target stream using `file.CopyToAsync(destination, cancellationToken)`.

---

### 5. Memory & Byte Sources

#### `FromBytes(ReadOnlyMemory<byte> data)`
Pushes a read-only memory buffer of bytes into the standard input pipe using `destination.WriteAsync(data, cancellationToken)`.

```csharp
public static PipeSource FromBytes(ReadOnlyMemory<byte> data)
```

#### `FromBytes(byte[] data)`
Overload accepting a byte array. Implicitly casts `data` to `ReadOnlyMemory<byte>`.

```csharp
public static PipeSource FromBytes(byte[] data)
```

#### `FromMemory(ReadOnlyMemory<byte> data)`
*Obsolete*. Pointers to `FromBytes(data)`. Decorated with `[Obsolete]` and `[ExcludeFromCodeCoverage]`.

```csharp
[Obsolete("Use FromBytes(ReadOnlyMemory<byte>) instead"), ExcludeFromCodeCoverage]
public static PipeSource FromMemory(ReadOnlyMemory<byte> data)
```

---

### 6. String-Based Sources

#### `FromString(string data, Encoding encoding)`
Converts the string into bytes using the supplied `Encoding` and delegates to `FromBytes(...)`.

```csharp
public static PipeSource FromString(string data, Encoding encoding)
```

#### `FromString(string data)`
Overload that defaults to `Console.InputEncoding`.

```csharp
public static PipeSource FromString(string data)
```

---

### 7. Command Piping (`FromCommand`)

Facilitates piping output from another command directly into the input pipe (equivalent to command-line piping: `cmdA | cmdB`).

#### `FromCommand(Command command, Func<Stream, Stream, CancellationToken, Task> copyStreamAsync)`

```csharp
public static PipeSource FromCommand(
    Command command,
    Func<Stream, Stream, CancellationToken, Task> copyStreamAsync
)
```
- **Parameters**:
  - `command`: The source command whose stdout will be read.
  - `copyStreamAsync`: A custom transformation or copying function taking the source output stream, destination input stream, and cancellation token.
- **Mechanism**: Configures the source command's standard output pipe using `WithStandardOutputPipe(...)` with a custom `PipeTarget`. Executes the command asynchronously and pipes output through `copyStreamAsync`.

#### `FromCommand(Command command)`

```csharp
public static PipeSource FromCommand(Command command)
```
- Overload that uses default stream copying (`source.CopyToAsync(destination, cancellationToken)`).

---

## Summary Matrix of Factory Methods

| Method / Property | Input Type | Description |
| :--- | :--- | :--- |
| `PipeSource.Null` | N/A | Sends no data (null device equivalent). |
| `Create` | `Func<Stream, CancellationToken, Task>` / `Action<Stream>` | Wraps arbitrary delegates as a pipe source. |
| `FromStream` | `Stream` (optional `bool autoFlush`) | Reads data from a readable stream. |
| `FromFile` | `string` (file path) | Asynchronously streams data from a file on disk. |
| `FromBytes` | `ReadOnlyMemory<byte>` / `byte[]` | Writes binary data directly from memory. |
| `FromMemory` | `ReadOnlyMemory<byte>` | *Obsolete*. Alias for `FromBytes`. |
| `FromString` | `string` (optional `Encoding`) | Encodes a text string to bytes and pipes it. |
| `FromCommand` | `Command` (optional transformer delegate) | Pipes standard output of a command into stdin. |