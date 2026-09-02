# Technical Documentation: `CliWrap.Utils.Channel<T>`

## Overview

The `CliWrap.Utils.Channel<T>` class is an internal, thread-safe asynchronous synchronization primitive designed to facilitate single-item bounded producer-consumer communication. It allows a producer to transmit items of type `T` asynchronously while a consumer receives those items asynchronously via an `IAsyncEnumerable<T>` stream. 

The implementation enforces a capacity of **1 item** at a time, providing backpressure mechanisms using two binary semaphores (`SemaphoreSlim`) and a single-item container (`Cell<T>`).

---

## Class Declaration

```csharp
namespace CliWrap.Utils;

internal class Channel<T> : IDisposable
```

* **Scope**: `internal`
* **Type Parameter**: `T` (unconstrained generic type)
* **Interfaces Implemented**: `System.IDisposable`

---

## Fields & State Management

The class maintains internal state using two `SemaphoreSlim` instances and a `Cell<T>` container from the `PowerKit` namespace:

| Field | Type | Initial Count | Max Count | Purpose |
| :--- | :--- | :--- | :--- | :--- |
| `_transmitLock` | `SemaphoreSlim` | `1` | `1` | Controls write/transmit permission. Prevents producers from pushing new items until the current item is consumed or the channel is closed. |
| `_receiveLock` | `SemaphoreSlim` | `0` | `0` (initially locked), max `1` | Signals read availability. Unlocks the consumer when an item is available or when a close signal is sent. |
| `_cell` | `Cell<T>` | N/A | Single slot | Holds the item currently being transmitted through the channel. |

---

## Methods

### `TransmitAsync`

```csharp
public async Task TransmitAsync(T item, CancellationToken cancellationToken = default)
```

Asynchronously writes an item to the channel.

* **Workflow**:
  1. Awaits `_transmitLock` asynchronously using the provided `cancellationToken` (configured with `.ConfigureAwait(false)`).
  2. Stores `item` into the `_cell` via `_cell.Store(item)`.
  3. Releases `_receiveLock` to signal to the reader that an item (or signal) is ready.
* **Backpressure**: If an item is already in the channel and has not been consumed yet, `_transmitLock` will block subsequent `TransmitAsync` or `CloseAsync` calls.

---

### `ReceiveAsync`

```csharp
public async IAsyncEnumerable<T> ReceiveAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default
)
```

Asynchronously yields items from the channel as an `IAsyncEnumerable<T>` stream.

* **Workflow**:
  1. Enters an infinite `while (true)` loop.
  2. Awaits `_receiveLock` asynchronously using `cancellationToken` (configured with `.ConfigureAwait(false)`).
  3. Attempts to read the cell contents using `_cell.TryOpen(out var item)`:
     * **If `TryOpen` returns `true` (Item available)**:
       * Yields the item (`yield return item`).
       * Clears the internal storage (`_cell.Clear()`).
       * Releases `_transmitLock` to allow the next item to be transmitted.
     * **If `TryOpen` returns `false` (Cell is empty)**:
       * Interprets an empty cell after a `_receiveLock` release as a signal that `CloseAsync()` was invoked.
       * Breaks out of the loop, completing the asynchronous enumeration.

---

### `CloseAsync`

```csharp
public async Task CloseAsync(CancellationToken cancellationToken = default)
```

Asynchronously signals that no more items will be transmitted and closes the channel.

* **Workflow**:
  1. Awaits `_transmitLock` asynchronously using `cancellationToken` (configured with `.ConfigureAwait(false)`).
  2. Clears `_cell` via `_cell.Clear()`.
  3. Releases `_receiveLock`.
* **Effect on Consumer**: When `ReceiveAsync` wakes up from `_receiveLock`, `_cell.TryOpen(...)` evaluates to `false`, causing `ReceiveAsync` to break its loop and complete enumeration.

---

### `Dispose`

```csharp
public void Dispose()
```

Releases all unmanaged resource allocations held by the internal semaphores.

* **Workflow**:
  1. Calls `Dispose()` on `_transmitLock`.
  2. Calls `Dispose()` on `_receiveLock`.

---

## Execution Flow & Lifecycle

```
[Producer]                                  [Channel<T>]                                  [Consumer]
    |                                            |                                            |
    |--- TransmitAsync(item) ------------------->|                                            |
    |    (Awaits _transmitLock)                  |                                            |
    |    (Stores item in _cell)                  |                                            |
    |    (Releases _receiveLock) --------------->|                                            |
    |                                            |<--- ReceiveAsync() ------------------------|
    |                                            |     (Awaits _receiveLock)                  |
    |                                            |     (Reads & clears _cell)                 |
    |                                            |     (Yields item to caller)                |
    |                                            |---- (Releases _transmitLock) ------------->|
    |                                            |                                            |
    |--- CloseAsync() -------------------------->|                                            |
    |    (Awaits _transmitLock)                  |                                            |
    |    (Clears _cell)                          |                                            |
    |    (Releases _receiveLock) --------------->|                                            |
    |                                            |     (Awaits _receiveLock)                  |
    |                                            |     (_cell.TryOpen fails)                  |
    |                                            |     (Breaks loop & ends enumeration) ------>|
```