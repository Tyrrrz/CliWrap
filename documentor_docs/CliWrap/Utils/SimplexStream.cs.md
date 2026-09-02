# Documentation: `CliWrap.Utils.SimplexStream`

## Overview

The `SimplexStream` class is an internal, asynchronous, in-memory stream designed for producer-consumer (simplex) operations. It inherits from `System.IO.Stream` and facilitates single-writer / single-reader data streaming using pooled memory buffers (`IMemoryOwner<byte>`) and semaphore locks (`SemaphoreSlim`).

Data written to the stream is temporarily held in a rented buffer until consumed by a reader. Synchronization mechanisms ensure that writers wait until readers finish consuming the written payload before submitting new data.

---

## Class Declaration

```csharp
namespace CliWrap.Utils;

internal partial class SimplexStream : Stream
```

* **Namespace**: `CliWrap.Utils`
* **Access Modifier**: `internal`
* **Class Modifiers**: `partial`
* **Base Class**: `System.IO.Stream`

---

## Internal State & Fields

| Field | Type | Description |
| :--- | :--- | :--- |
| `_writeLock` | `SemaphoreSlim` | Initialized to `(1, 1)`. Controls write access so that only one write operation can occur at a time, and blocks subsequent writes until the reader finishes reading the active buffer. |
| `_readLock` | `SemaphoreSlim` | Initialized to `(0, 1)`. Blocks read operations until data has been written to the buffer. |
| `_buffer` | `IMemoryOwner<byte>?` | A managed buffer rented from `MemoryPool<byte>.Shared` holding the active unconsumed chunk of written data. |
| `_bufferWritten` | `int` | Tracks the total number of bytes written to the current `_buffer`. |
| `_bufferRead` | `int` | Tracks the total number of bytes read from the current `_buffer`. |
| `_isCompleted` | `volatile bool` | A flag indicating if the stream has reached the end-of-stream state (latched when an empty buffer is written). |

---

## Core Operational Logic

### 1. Synchronization Flow
`SimplexStream` coordinates production and consumption using two `SemaphoreSlim` instances:
1. **Write Phase**:
   - The writer waits for `_writeLock`.
   - Data is copied into a buffer rented from `MemoryPool<byte>.Shared`.
   - Indices `_bufferWritten` and `_bufferRead` are initialized.
   - If the incoming payload is empty (`buffer.IsEmpty`), `_isCompleted` is set to `true`.
   - `_readLock` is released to allow the reader to proceed.
2. **Read Phase**:
   - If `_isCompleted` is already `true`, `ReadAsync` immediately returns `0` (indicating end of stream).
   - Otherwise, the reader waits on `_readLock`.
   - Data is copied from `_buffer` into the destination buffer.
   - If all data in `_buffer` has been consumed (`_bufferRead >= _bufferWritten`):
     - If the stream is not completed (`!_isCompleted`), `_writeLock` is released, allowing the next write operation.
     - If completed, `_writeLock` is not released.
   - If data still remains in `_buffer`, `_readLock` is released again to allow subsequent read calls to consume the rest of the current buffer.

---

## Method Details

### Asynchronous Write Operations

```csharp
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
public override
#else
private
#endif
async ValueTask WriteAsync(
    ReadOnlyMemory<byte> buffer,
    CancellationToken cancellationToken = default
)
```
* **Behavior**:
  1. Acquires `_writeLock`.
  2. Compares `_buffer` length against `buffer.Length`. Disposes the old buffer and rents a new buffer from `MemoryPool<byte>.Shared` if required.
  3. Copies data from `buffer` into `_buffer.Memory`.
  4. Sets `_bufferWritten = buffer.Length` and `_bufferRead = 0`.
  5. Latches `_isCompleted = true` if `buffer.IsEmpty`.
  6. Releases `_readLock`.

---

### Asynchronous Read Operations

```csharp
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
public override
#else
private
#endif
async ValueTask<int> ReadAsync(
    Memory<byte> buffer,
    CancellationToken cancellationToken = default
)
```
* **Behavior**:
  1. Checks `_isCompleted`. If `true`, returns `0`.
  2. Acquires `_readLock`.
  3. Calculates slice size: `Math.Min(buffer.Length, _bufferWritten - _bufferRead)`.
  4. Copies data to `buffer` and increments `_bufferRead`.
  5. Determines lock release:
     - If `_bufferRead >= _bufferWritten`: Releases `_writeLock` (unless `_isCompleted` is true).
     - Otherwise: Re-releases `_readLock` for remaining bytes.
  6. Returns the number of bytes read.

---

### End of Stream and Close

```csharp
public async Task CloseAsync(CancellationToken cancellationToken = default)
```
* **Behavior**:
  - Calls `WriteAsync(ReadOnlyMemory<byte>.Empty, cancellationToken)`.
  - Writing an empty memory region sets `_isCompleted = true`, signaling end-of-stream (returns `0` on read).

---

### Target Framework Compatibilities (Polyfills)

For target frameworks older than .NET Standard 2.1 / .NET Core 2.1:
- `WriteAsync(ReadOnlyMemory<byte>, ...)` and `ReadAsync(Memory<byte>, ...)` are implemented as `private` helper methods.
- Array-based `Task WriteAsync(byte[], int, int, ...)` and `Task<int> ReadAsync(byte[], int, int, ...)` are overridden to forward calls to the memory-based helper methods using `.AsMemory(...)`.

---

### Resource Cleanup

```csharp
protected override void Dispose(bool disposing)
```
* **Behavior**:
  When `disposing` is `true`:
  - Disposes `_readLock`.
  - Disposes `_writeLock`.
  - Disposes `_buffer` (releasing memory back to `MemoryPool<byte>.Shared`).
  - Calls `base.Dispose(disposing)`.

---

## Stream Overrides and Members

The second partial definition of `SimplexStream` provides standard required implementations for `System.IO.Stream` abstract/virtual members:

| Member | Implementation / Return Value |
| :--- | :--- |
| `CanRead` | `true` |
| `CanSeek` | `false` |
| `CanWrite` | `true` |
| `Position` (Get) | Returns `_bufferRead` |
| `Position` (Set) | Throws `NotSupportedException` |
| `Length` | Returns `_bufferWritten` |
| `Read(byte[], int, int)` | Synchronously waits on `ReadAsync` |
| `Write(byte[], int, int)` | Synchronously waits on `WriteAsync` |
| `Flush()` | Throws `NotSupportedException` |
| `Seek(long, SeekOrigin)` | Throws `NotSupportedException` |
| `SetLength(long)` | Throws `NotSupportedException` |

> *Note: These overridden standard stream members are marked with `[ExcludeFromCodeCoverage]` as they are unused or mandatory boilerplate.*