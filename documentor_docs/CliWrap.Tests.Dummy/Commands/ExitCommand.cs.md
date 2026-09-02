# Technical Documentation: `ExitCommand.cs`

**File Path:** `CliWrap.Tests.Dummy/Commands/ExitCommand.cs`  
**Namespace:** `CliWrap.Tests.Dummy.Commands`

---

## Overview

The `ExitCommand` class is a CLI command implementation built using the **CliFx** framework. It belongs to the dummy test application used in `CliWrap.Tests`. 

Its primary purpose is to simulate process termination with a specified exit code. When executed with a non-zero exit code, it throws a `CommandException` containing that code, causing the command-line application to exit with the requested status.

---

## Key Components

### 1. Class Attributes and Declarations

```csharp
[Command("exit")]
public partial class ExitCommand : ICommand
```

* **`[Command("exit")]`**: Registers this class as a CliFx command associated with the command name `exit`.
* **`ICommand`**: The CliFx interface that requires implementation of the `ExecuteAsync` method.
* **`partial`**: Allows the class definition to be split across multiple files if needed (though only this definition is present in the file).

---

### 2. Command Parameters

```csharp
[CommandParameter(0)]
public int ExitCode { get; set; }
```

* **`ExitCode`** (`int`): A positional command parameter (at index `0`). This value represents the exit code that the command should produce.

---

### 3. Methods

#### `ExecuteAsync`

```csharp
public ValueTask ExecuteAsync(IConsole console)
```

Implements the execution logic for the `ICommand` interface.

* **Parameters:**
  * `console` (`IConsole`): An abstraction provided by CliFx representing the console interface (not directly used within this method).
* **Return Value:** `ValueTask` representing the asynchronous execution status.

---

## How It Works

1. **Parameter Binding:** When the CLI application runs the `exit` command, CliFx parses the first positional argument into the `ExitCode` property.
2. **Execution Check:**
   * **If `ExitCode` is non-zero (`ExitCode != 0`):** The method throws a `CliFx.Exceptions.CommandException` formatted as:
     * **Message:** `"Exit code set to {ExitCode}"`
     * **Exit Code:** `ExitCode`
     Handling this exception causes CliFx to set the process exit code accordingly.
   * **If `ExitCode` is zero (`ExitCode == 0`):** The method bypasses the exception and returns `default` (`ValueTask`), indicating a successful, clean completion with exit code `0`.