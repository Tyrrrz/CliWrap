# Technical Documentation: `CliWrap.Native.WindowsSignaler`

## Overview

The `WindowsSignaler` class in the `CliWrap.Native` namespace provides a mechanism for sending native signals to target processes on Windows environments. It achieves this by extracting a temporary helper executable (`CliWrap.Signaler.exe`) embedded within the assembly resources, executing it with process and signal identifiers, and cleaning up the executable when disposed.

---

## Class Signature

```csharp
namespace CliWrap.Native;

internal partial class WindowsSignaler : IDisposable
```

* **Access Modifier:** `internal`
* **Class Type:** `partial`
* **Interfaces:** `IDisposable`

---

## Key Components

### Primary Constructor

```csharp
internal partial class WindowsSignaler(string filePath) : IDisposable
```

Accepts a single parameter:
* `filePath` (`string`): The absolute file path to the extracted signaler executable.

---

### Methods

#### `Deploy()`

```csharp
public static WindowsSignaler Deploy()
```

Deploys the embedded helper executable to the operating system's temporary directory and returns a new `WindowsSignaler` instance.

* **Behavior:**
  1. Generates a unique temporary file path using `Path.GetTempPath()` formatted as `CliWrap.Signaler.{Guid.NewGuid()}.exe`.
  2. Extracts the embedded manifest resource `CliWrap.Signaler.exe` to the temporary file path using the `ExtractManifestResource` extension method from `PowerKit.Extensions`.
  3. Returns a initialized `WindowsSignaler` instance pointing to the newly extracted file.

---

#### `TrySend(int processId, int signalId)`

```csharp
public bool TrySend(int processId, int signalId)
```

Attempts to send a specified signal to a target process using the deployed helper executable.

* **Parameters:**
  * `processId` (`int`): The target process ID.
  * `signalId` (`int`): The integer identifier of the signal to send.

* **Execution Flow:**
  1. Instantiates a standard `System.Diagnostics.Process`.
  2. Configures `ProcessStartInfo`:
     * `FileName`: Set to the `filePath` of the signaler executable.
     * `Arguments`: `<processId> <signalId>` (formatted using `CultureInfo.InvariantCulture`).
     * `CreateNoWindow`: `true` (runs hidden without opening a console window).
     * `UseShellExecute`: `false`.
     * `Environment["COMPLUS_OnlyUseLatestCLR"]`: Set to `"1"`. This configures framework rollover so the .NET 3.5 executable can execute under .NET 4.0 or higher.
  3. Launches the process via `process.Start()`. Returns `false` if initialization fails.
  4. Waits up to **30 seconds** for completion via `process.WaitForExit(TimeSpan.FromSeconds(30))`. Returns `false` if execution times out.
  5. Returns `true` if `process.ExitCode == 0`; otherwise returns `false`.

* **Return Value:**
  * `bool`: `true` if the signal execution process completed successfully with an exit code of `0`; otherwise `false`.

---

#### `Dispose()`

```csharp
public void Dispose()
```

Performs cleanup by deleting the extracted executable file from the file system.

* **Behavior:**
  * Calls `File.Delete(filePath)`.
  * If file deletion throws an exception, it catches the error and executes `Debug.Fail("Failed to delete the signaler executable.")`.

---

## Dependencies & Imports

* `System`: Core system types and `IDisposable`.
* `System.Diagnostics`: Provides `Process`, `ProcessStartInfo`, and `Debug`.
* `System.Globalization`: Provides `CultureInfo` for culture-invariant integer string formatting.
* `System.IO`: Provides `Path` and `File` operations.
* `System.Reflection`: Provides `Assembly` reflection utilities.
* `PowerKit.Extensions`: Provides the `ExtractManifestResource` extension method on `Assembly`.

---

## Lifecycle Workflow

```
[ Call WindowsSignaler.Deploy() ]
               │
               ▼
[ Extract 'CliWrap.Signaler.exe' to Temp Directory ]
               │
               ▼
[ Instantiate WindowsSignaler(filePath) ]
               │
               ▼
[ Call TrySend(processId, signalId) ]
               │
               ├─► Spawns helper process with env COMPLUS_OnlyUseLatestCLR="1"
               ├─► Waits max 30 seconds
               └─► Returns true if ExitCode == 0, false otherwise
               │
               ▼
[ Call Dispose() ]
               │
               ▼
[ Delete temporary helper file from disk ]
```