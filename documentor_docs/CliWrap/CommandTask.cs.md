# Technical Documentation: `CliWrap.CommandTask<TResult>`

## Overview

The `CommandTask<TResult>` class in the `CliWrap` namespace represents an asynchronous execution of a command. It wraps a standard .NET `Task<TResult>` while exposing additional context specific to process execution, notably the underlying process ID (`ProcessId`).

It implements `IDisposable` and provides custom awaiter support, allowing instances of `CommandTask<TResult>` to be awaited directly using `await` syntax or implicitly converted to a standard `Task<TResult>`.

---

## Class Declaration

```csharp
namespace CliWrap;

public partial class CommandTask<TResult>(Task<TResult> task, int processId) : IDisposable
```

### Type Parameters
* **`TResult`**: The return type produced when the command task completes.

### Implemented Interfaces
* **`IDisposable`**: Enables disposal of the underlying `Task` resources.

### Primary Constructor Parameters
* **`task`** (`Task<TResult>`): The underlying asynchronous task representing command execution.
* **`processId`** (`int`): The OS process identifier (PID) associated with the executed command.

---

## Properties

### `Task`
```csharp
public Task<TResult> Task { get; }
```
* **Description**: Gets the underlying `Task<TResult>` representing the asynchronous execution.

### `ProcessId`
```csharp
public int ProcessId { get; }
```
* **Description**: Gets the operating system process ID (PID) corresponding to the command execution.

---

## Public Methods

### `GetAwaiter()`
```csharp
public TaskAwaiter<TResult> GetAwaiter()
```
* **Description**: Returns the awaiter for the underlying `Task`.
* **Purpose**: Enables the C# `await` keyword to be used directly on a `CommandTask<TResult>` instance without explicitly referencing the `.Task` property.
* **Return Value**: `TaskAwaiter<TResult>`

### `ConfigureAwait(...)`
```csharp
public ConfiguredTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext)
```
* **Description**: Configures an awaiter used to await the underlying task.
* **Parameters**:
  * `continueOnCapturedContext` (`bool`): `true` to attempt to marshal the continuation back to the original context captured; otherwise, `false`.
* **Return Value**: `ConfiguredTaskAwaitable<TResult>`

### `Dispose()`
```csharp
public void Dispose()
```
* **Description**: Disposes the underlying `Task` object by invoking `Task.Dispose()`.

### `Select<T>(...)` [Obsolete]
```csharp
[Obsolete("Use async/await instead."), ExcludeFromCodeCoverage]
public CommandTask<T> Select<T>(Func<TResult, T> transform)
```
* **Description**: Lazily transforms the result of the task using the provided delegate function.
* **Attributes**:
  * `[Obsolete("Use async/await instead.")]`: Marked as obsolete; callers should use standard `async`/`await` patterns instead.
  * `[ExcludeFromCodeCoverage]`: Excluded from code coverage metrics.
* **Parameters**:
  * `transform` (`Func<TResult, T>`): A delegate to transform the `TResult` to type `T`.
* **Return Value**: A new `CommandTask<T>` containing the transformed result.

---

## Conversion Operators

### Implicit Conversion to `Task<TResult>`
```csharp
public static implicit operator Task<TResult>(CommandTask<TResult> commandTask)
```
* **Description**: Defines an implicit conversion from a `CommandTask<TResult>` instance to a standard `Task<TResult>`.
* **Behavior**: Returns the underlying `commandTask.Task`. Allows passing a `CommandTask<TResult>` directly to methods accepting a `Task<TResult>`.

---

## Internal Methods

### `Wrap<T>(Func<CommandTask<TResult>, CommandTask<T>> transform)`
```csharp
internal CommandTask<T> Wrap<T>(Func<CommandTask<TResult>, CommandTask<T>> transform)
```
* **Description**: Applies a transformation delegate directly to `this` instance. Allows chaining operations on the custom task.

### `Wrap<T>(Func<CommandTask<TResult>, Task<T>> transform)`
```csharp
internal CommandTask<T> Wrap<T>(Func<CommandTask<TResult>, Task<T>> transform)
```
* **Description**: Accepts a transformation delegate that returns a `Task<T>` and wraps the resulting task in a new `CommandTask<T>`, preserving the original `ProcessId`.