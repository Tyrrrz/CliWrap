# Technical Documentation: `SleepCommand.cs`

**File Path:** `CliWrap.Tests.Dummy/Commands/SleepCommand.cs`  
**Namespace:** `CliWrap.Tests.Dummy.Commands`  

---

## Overview

The `SleepCommand` class is a command implementation using the **CliFx** framework. Its primary purpose is to simulate an asynchronous delay (sleep) operation for a specified duration. It includes support for cancellation handling via the console infrastructure.

---

## Class Definition

```csharp
[Command("sleep")]
public partial class SleepCommand : ICommand
```

* **Interfaces Implemented:** `ICommand` (CliFx framework interface for executable CLI commands).
* **Attributes:**
  * `[Command("sleep")]`: Registers the command with the name `"sleep"`.

---

## Command Parameters

### `Duration`

* **Type:** `TimeSpan`
* **Attribute:** `[CommandParameter(0)]`
* **Default Value:** `TimeSpan.FromSeconds(1)` (1 second)
* **Description:** Represents the time interval for which the command will pause execution. Configured as the first positional command parameter (index `0`).

---

## Methods

### `ExecuteAsync`

```csharp
public async ValueTask ExecuteAsync(IConsole console)
```

Executes the command logic asynchronously when invoked by the CliFx framework.

#### Parameters:
* **`console`** (`IConsole`): The console abstraction provided by CliFx to handle input, output, and cancellation.

#### Execution Flow:

1. **Cancellation Handler Registration:**  
   Obtains a `CancellationToken` by calling `console.RegisterCancellationHandler()`. This allows the execution to respond to interruption signals (e.g., `Ctrl+C`).

2. **Execution Message:**  
   Writes the initial status message to the console standard output stream:
   ```text
   Sleeping for {Duration}...
   ```

3. **Asynchronous Delay:**  
   Executes `await Task.Delay(Duration, cancellationToken)` to pause execution for the specified `Duration`.

4. **Exception Handling (`OperationCanceledException`):**  
   If a cancellation request occurs during `Task.Delay`:
   * Catches `OperationCanceledException`.
   * Writes `"Canceled."` to the console output.
   * Immediately returns, terminating execution early.

5. **Completion Message:**  
   If the delay completes successfully without being canceled, writes `"Done."` to the console output.

---

## Console Output Summary

| Scenario | Standard Output Messages |
| :--- | :--- |
| **Normal Completion** | `Sleeping for <Duration>...`<br>`Done.` |
| **Canceled Execution** | `Sleeping for <Duration>...`<br>`Canceled.` |