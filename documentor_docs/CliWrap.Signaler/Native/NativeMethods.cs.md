# Technical Documentation: `CliWrap.Signaler/Native/NativeMethods.cs`

## Overview

The `NativeMethods.cs` file provides unmanaged Platform Invoke (P/Invoke) bindings for Windows API functions exposed by `kernel32.dll`. These functions enable low-level management of Windows console sessions, control event handling, and console signaling between process groups.

---

## File Identification

* **File Path:** `CliWrap.Signaler/Native/NativeMethods.cs`
* **Namespace:** `CliWrap.Signaler.Native`
* **Access Modifier:** `internal static`

---

## Class Architecture

```
CliWrap.Signaler.Native (Namespace)
└── NativeMethods (internal static class)
    └── Windows (public static class)
        ├── FreeConsole()
        ├── AttachConsole(uint)
        ├── ConsoleCtrlDelegate (delegate)
        ├── SetConsoleCtrlHandler(ConsoleCtrlDelegate?, bool)
        └── GenerateConsoleCtrlEvent(uint, uint)
```

---

## Key Components & API Reference

### `NativeMethods` Class
An `internal static` container class designed to house native interop method definitions.

---

### `NativeMethods.Windows` Class
A `public static` nested class containing P/Invoke declarations specifically for the Windows operating system via `kernel32.dll`.

#### All imports use the attribute `[DllImport("kernel32.dll", SetLastError = true)]`.

---

### Delegates

#### `ConsoleCtrlDelegate`
```csharp
public delegate bool ConsoleCtrlDelegate(uint dwCtrlEvent);
```
* **Purpose:** Represents a callback delegate function that handles console control signals sent to the application.
* **Parameters:**
  * `dwCtrlEvent` (`uint`): The signal/event type received by the process.
* **Return Value:** `bool` — Returns `true` if the signal was handled; `false` to pass the signal to the next handler.

---

### Native Methods

#### 1. `FreeConsole`
```csharp
[DllImport("kernel32.dll", SetLastError = true)]
public static extern bool FreeConsole();
```
* **Purpose:** Detaches the calling process from its current console session, if it is attached to one.
* **Parameters:** None.
* **Return Value:** `bool` — `true` if the function succeeds; otherwise, `false`.

---

#### 2. `AttachConsole`
```csharp
[DllImport("kernel32.dll", SetLastError = true)]
public static extern bool AttachConsole(uint dwProcessId);
```
* **Purpose:** Attaches the calling process to the console of the specified process.
* **Parameters:**
  * `dwProcessId` (`uint`): The identifier of the target process whose console is to be attached, or `ATTACH_PARENT_PROCESS` (`(uint)-1`).
* **Return Value:** `bool` — `true` if the attachment succeeds; otherwise, `false`.

---

#### 3. `SetConsoleCtrlHandler`
```csharp
[DllImport("kernel32.dll", SetLastError = true)]
public static extern bool SetConsoleCtrlHandler(
    ConsoleCtrlDelegate? handlerRoutine,
    bool add
);
```
* **Purpose:** Adds or removes an application-defined handler routine (`ConsoleCtrlDelegate`) from the invocation list for the calling process.
* **Parameters:**
  * `handlerRoutine` (`ConsoleCtrlDelegate?`): Pointer to the delegate function to add or remove. If `null`, it changes the process's reaction to CTRL+C signals.
  * `add` (`bool`): If `true`, the handler is added; if `false`, the handler is removed.
* **Return Value:** `bool` — `true` if successful; otherwise, `false`.

---

#### 4. `GenerateConsoleCtrlEvent`
```csharp
[DllImport("kernel32.dll", SetLastError = true)]
public static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
```
* **Purpose:** Sends a specified signal (such as `CTRL_C_EVENT` or `CTRL_BREAK_EVENT`) to a console process group that shares the console associated with the calling process.
* **Parameters:**
  * `dwCtrlEvent` (`uint`): The type of signal to generate.
  * `dwProcessGroupId` (`uint`): The ID of the target process group receiving the signal.
* **Return Value:** `bool` — `true` if the signal was sent successfully; otherwise, `false`.

---

## Interop Configuration Details

| Attribute / Property | Value | Description |
| :--- | :--- | :--- |
| **Target DLL** | `"kernel32.dll"` | Windows Core System API Library. |
| **`SetLastError`** | `true` | Instructs the runtime to capture the error code set by `SetLastError()` in Windows API calls, accessible via `Marshal.GetLastWin32Error()`. |