# Technical Documentation: `ProcessExtensions.cs`

**File Path:** `CliWrap/Utils/Extensions/ProcessExtensions.cs`  
**Namespace:** `CliWrap.Utils.Extensions`  
**Access Modifier:** `internal static`  

---

## Overview

The `ProcessExtensions` class provides internal utility extension members for the standard `System.Diagnostics.Process` class. Its primary purpose is to abstract platform-specific process termination and interrupt logic (such as sending `SIGINT` or `SIGKILL` signals) and to simplify retrieving the executable name from a process's start configuration.

---

## Dependencies & Imports

* **`System`**: Provides core platform detection APIs (`OperatingSystem.IsWindows()`, `OperatingSystem.IsLinux()`, `OperatingSystem.IsMacOS()`).
* **`System.Diagnostics`**: Provides the base `Process` class.
* **`System.IO`**: Provides `Path.GetFileName` for extracting file names from paths.
* **`CliWrap.Native`**: Provides native platform interaction helpers (`WindowsSignaler` and `NativeMethods.Unix`).

---

## Extension Members

`ProcessExtensions` defines an explicit extension block targeting `System.Diagnostics.Process`.

```csharp
extension(Process process)
```

### 1. `FileName` Property

```csharp
public string FileName => Path.GetFileName(process.StartInfo.FileName);
```

* **Type:** `string` (Read-only property)
* **Description:** Extracts and returns the file name (including extension) from the process's `StartInfo.FileName` path.
* **How it works:** Calls `Path.GetFileName()` passing the `process.StartInfo.FileName` string.

---

### 2. `TryInterrupt()` Method

```csharp
public bool TryInterrupt()
```

* **Return Type:** `bool` (`true` if the signal was successfully dispatched; `false` otherwise)
* **Description:** Attempts to gracefully interrupt the process by sending a `SIGINT` signal (or its equivalent Ctrl+C signal).
* **How it works:**
  1. **Windows Platform (`OperatingSystem.IsWindows()`):**
     * Deploys a native helper (`WindowsSignaler.Deploy()`).
     * Calls `signaler.TrySend(process.Id, 0)` to send a Ctrl+C signal to the target process ID.
  2. **Unix Platforms (`OperatingSystem.IsLinux()` or `OperatingSystem.IsMacOS()`):**
     * Directly calls native Unix interop method `NativeMethods.Unix.Kill(process.Id, 2)`. Signal `2` corresponds to `SIGINT`.
     * Returns `true` if the return code is `0` (indicating success).
  3. **Unsupported Platforms:**
     * Returns `false` if the OS is neither Windows, Linux, nor macOS.
  4. **Error Handling:**
     * Wrapped in a `try...catch` block. If any exception occurs during signal dispatch, it catches the exception and returns `false`.

---

### 3. `TryKill()` Method

```csharp
public bool TryKill(bool entireProcessTree = true)
```

* **Parameters:**
  * `entireProcessTree` (`bool`, default: `true`): Specifies whether to terminate the target process along with all of its child processes.
* **Return Type:** `bool` (`true` if termination succeeded without exception; `false` otherwise)
* **Description:** Attempts to forcefully terminate the process (`SIGKILL` equivalent).
* **How it works:**
  1. Calls the standard `process.Kill(entireProcessTree)` method provided by `System.Diagnostics.Process`.
  2. Returns `true` if the operation completes without throwing an exception.
  3. **Error Handling:** Returns `false` if an exception is thrown (e.g., if the process has already exited or access is denied).

---

## Summary of Logic Flow

| Action | Target OS | Underlying Execution Logic | Catch Handler Behavior |
| :--- | :--- | :--- | :--- |
| **`FileName`** | All | `Path.GetFileName(process.StartInfo.FileName)` | N/A |
| **`TryInterrupt()`** | Windows | `WindowsSignaler.Deploy()` $\rightarrow$ `TrySend(process.Id, 0)` | Returns `false` |
| **`TryInterrupt()`** | Linux / macOS | `NativeMethods.Unix.Kill(process.Id, 2) == 0` | Returns `false` |
| **`TryInterrupt()`** | Other OS | Unsupported | Returns `false` |
| **`TryKill()`** | All | `process.Kill(entireProcessTree)` | Returns `false` |