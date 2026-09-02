# Technical Documentation: `CliWrap.Signaler/Program.cs`

## Overview

The `CliWrap.Signaler` utility is a lightweight Windows-focused command-line tool designed to send native Windows console signals (such as `CTRL_C_EVENT` or `CTRL_BREAK_EVENT`) to a target process specified by its Process ID (PID). 

This tool serves as an isolated helper executable that attaches to a target process's console session and invokes native Win32 APIs to deliver console control events.

---

## Attribution & License Reference

The implementation logic references `MedallionShell` by Michael Adelson (MIT License):
- **Source Reference**: [MedallionShell.ProcessSignaler/Signals/Signaler.cs](https://github.com/madelson/MedallionShell/blob/3fddb89860842ffc836a0d0f69b161f67e4aa7c4/MedallionShell.ProcessSignaler/Signals/Signaler.cs)

---

## Class Architecture

### `namespace CliWrap.Signaler`

#### `public static class Program`
Serves as the main entry point for the signaling executable.

---

## Entry Point: `Main` Method

```csharp
public static int Main(string[] args)
```

### Parameters

| Argument Index | Parameter Name | Type | Description |
| :--- | :--- | :--- | :--- |
| `args[0]` | `processId` | `int` | The Process ID (PID) of the target process to receive the console signal. Parsed using `CultureInfo.InvariantCulture`. |
| `args[1]` | `signalId` | `int` | The Windows Console Control Event identifier (signal ID) to send to the console session. Parsed using `CultureInfo.InvariantCulture`. |

---

## Program Logic & Workflow

The execution flow follows a sequential Win32 API sequence:

1. **Argument Parsing**:
   Extracts `processId` and `signalId` from command-line arguments using `int.Parse` with `CultureInfo.InvariantCulture`.

2. **Detach Current Console (`FreeConsole`)**:
   Calls `NativeMethods.Windows.FreeConsole()` to detach the signaler executable from its own console window (if attached). Any error from this operation is ignored because the process might not be attached to an existing console.

3. **Chain Native Operations (`isSuccess`)**:
   Executes three Win32 API calls sequentially using logical AND (`&&`) short-circuiting:
   - **`AttachConsole((uint)processId)`**: Attaches the current process to the console of the target process specified by `processId`.
   - **`SetConsoleCtrlHandler(null, true)`**: Configures the current process to ignore incoming console signals (`null` handler parameter with `true` to add the ignore attribute). This prevents the signaler process itself from prematurely terminating when the signal is generated.
   - **`GenerateConsoleCtrlEvent((uint)signalId, 0)`**: Emits the console control event (`signalId`) to all processes attached to the target console session (using `dwProcessGroupId = 0`).

4. **Return Result**:
   - Returns `0` if all native calls in the chain succeeded (`isSuccess == true`).
   - Returns the Win32 error code via `Marshal.GetLastWin32Error()` if any call in the chain returned `false`.

---

## Dependencies & Imports

- **`System.Globalization`**: Provides `CultureInfo.InvariantCulture` for argument parsing.
- **`System.Runtime.InteropServices`**: Provides `Marshal.GetLastWin32Error()` for error reporting.
- **`CliWrap.Signaler.Native`**: Contains the interop binding class `NativeMethods.Windows`.

---

## Native Methods Referenced

The class relies on native Windows API functions wrapped in `NativeMethods.Windows`:

* `FreeConsole()`
* `AttachConsole(uint dwProcessId)`
* `SetConsoleCtrlHandler(PHANDLER_ROUTINE HandlerRoutine, bool Add)` (invoked with `null` handler and `true`)
* `GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId)`

---

## Exit Codes

| Return Code | Description |
| :--- | :--- |
| `0` | Success. The signal was successfully attached and generated. |
| Non-Zero (`Win32 Error`) | Failure. Returns the Win32 error code corresponding to the last failed native function call obtained via `Marshal.GetLastWin32Error()`. |