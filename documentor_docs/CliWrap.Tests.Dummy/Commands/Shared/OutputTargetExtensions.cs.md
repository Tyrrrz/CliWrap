# Technical Documentation: `OutputTargetExtensions.cs`

## Overview

The `OutputTargetExtensions.cs` file defines an internal static class containing an extension method for the `IConsole` interface (provided by `CliFx.Infrastructure`). Its primary purpose is to inspect a given `OutputTarget` bitwise flag enum and return the matching stream writers (`ConsoleWriter`) associated with the console's standard output (`StdOut`) and standard error (`StdErr`) streams.

---

## File Identification

* **File Path:** `CliWrap.Tests.Dummy/Commands/Shared/OutputTargetExtensions.cs`
* **Namespace:** `CliWrap.Tests.Dummy.Commands.Shared`
* **Access Modifier:** `internal static`

---

## External Dependencies

* `System.Collections.Generic`: Provides `IEnumerable<T>` for lazily yielding output stream writers.
* `CliFx.Infrastructure`: Provides the `IConsole` interface and its standard stream properties (`Output` and `Error`).

---

## Class & Extension Members

### `OutputTargetExtensions` Class

```csharp
internal static class OutputTargetExtensions
```

An internal, static container class for extension logic applied to `IConsole`.

---

### Extension Syntax & Method Breakdown

#### Extension Receiver Definition
```csharp
extension(IConsole console)
```
Defines an extension block scoped to instances implementing `IConsole`.

---

#### Method: `GetWriters`

```csharp
public IEnumerable<ConsoleWriter> GetWriters(OutputTarget target)
```

Retrieves an enumerable collection of `ConsoleWriter` instances corresponding to the flags set on the `OutputTarget` parameter.

* **Parameters:**
  * `target` (`OutputTarget`): A bitwise flag indicating which output streams are targeted (`StdOut`, `StdErr`, or both).

* **Return Type:**
  * `IEnumerable<ConsoleWriter>`: An iterator that yields the standard output stream (`console.Output`), standard error stream (`console.Error`), or both based on the `target` parameter.

---

## Execution Logic & Workflow

The method uses standard `yield return` logic to lazily yield `ConsoleWriter` streams:

1. **Check standard output flag:**
   * Evaluates `target.HasFlag(OutputTarget.StdOut)`.
   * If `true`, yields `console.Output`.

2. **Check standard error flag:**
   * Evaluates `target.HasFlag(OutputTarget.StdErr)`.
   * If `true`, yields `console.Error`.

Depending on the flags set on `target`, the iterator can yield:
* No writers (if neither flag is set).
* Only `console.Output` (if only `OutputTarget.StdOut` is set).
* Only `console.Error` (if only `OutputTarget.StdErr` is set).
* Both `console.Output` and `console.Error` sequentially (if both flags are set).