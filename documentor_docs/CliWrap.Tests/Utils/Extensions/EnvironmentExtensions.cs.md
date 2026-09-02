# Technical Documentation: `EnvironmentExtensions.cs`

**File Path:** `CliWrap.Tests/Utils/Extensions/EnvironmentExtensions.cs`  
**Namespace:** `CliWrap.Tests.Utils.Extensions`  
**Access Modifier:** `internal static`

---

## Overview

The `EnvironmentExtensions` class provides static extension methods for C#'s `System.Environment` class. Its primary purpose is to facilitate temporary modifications to process environment variables—such as setting custom environment variable values or appending entries to the `PATH` variable—and automatically reverting those changes when the returned `IDisposable` instance is disposed.

---

## Dependencies

- **`System`**: Provides access to `Environment`, `IDisposable`, and base language types.
- **`System.IO`**: Provides `Path.PathSeparator` to format OS-specific path delimiters.
- **`PowerKit`**: Provides `Disposable.Create(...)` to construct `IDisposable` instances from cleanup actions.

---

## Class Structure

```csharp
namespace CliWrap.Tests.Utils.Extensions;

internal static class EnvironmentExtensions
{
    extension(Environment)
    {
        // Extension methods defined here
    }
}
```

The class uses static extension block syntax (`extension(Environment)`) to target the `System.Environment` class.

---

## Methods Breakdown

### 1. `SetTempEnvironmentVariable`

Temporarily sets an environment variable to a specified value and returns an `IDisposable` that restores the original value upon disposal.

#### Signature
```csharp
public static IDisposable SetTempEnvironmentVariable(string name, string? value)
```

#### Parameters
- **`name`** (`string`): The name of the environment variable to modify.
- **`value`** (`string?`): The target value to set. Can be `null` to clear the variable.

#### Returns
- **`IDisposable`**: An object created via `PowerKit.Disposable.Create`. Disposing this object resets the environment variable to its previous state prior to calling this method.

#### Internal Execution Flow
1. Captures the current value of the environment variable via `Environment.GetEnvironmentVariable(name)` into `lastValue`.
2. Updates the environment variable to `value` using `Environment.SetEnvironmentVariable(name, value)`.
3. Returns a disposable object that invokes `Environment.SetEnvironmentVariable(name, lastValue)` when disposed.

---

### 2. `ExtendPath`

Temporarily appends a specified folder or file path to the system's `PATH` environment variable.

#### Signature
```csharp
public static IDisposable ExtendPath(string path)
```

#### Parameters
- **`path`** (`string`): The file system path to append to the existing `PATH` environment variable.

#### Returns
- **`IDisposable`**: An object that, when disposed, restores the `PATH` environment variable to its state prior to the extension call.

#### Internal Execution Flow
1. Reads the current `PATH` environment variable via `Environment.GetEnvironmentVariable("PATH")`.
2. Concatenates the existing `PATH` string, the platform-specific separator (`Path.PathSeparator`), and the provided `path`.
3. Delegates to `SetTempEnvironmentVariable("PATH", ...)` and returns the resulting `IDisposable`.

---

## Summary of Usage Logic

Both methods follow a scoped modification pattern using `IDisposable`:

```
[Call Method] ──> [Save Current Value] ──> [Set New Value] ──> [Return IDisposable]
                                                                        │
                                                                 (Upon Disposal)
                                                                        │
                                                                        ▼
                                                             [Restore Saved Value]
```