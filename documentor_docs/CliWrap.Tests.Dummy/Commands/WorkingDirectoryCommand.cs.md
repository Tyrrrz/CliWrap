# Technical Documentation: `WorkingDirectoryCommand.cs`

**File Path:** `CliWrap.Tests.Dummy/Commands/WorkingDirectoryCommand.cs`

## Overview

The `WorkingDirectoryCommand` class represents a CLI command designed to output the application's current working directory to the console standard output stream. It is defined within the `CliWrap.Tests.Dummy` executable project and utilizes the `CliFx` framework for CLI command parsing and execution.

---

## Code Overview & Purpose

The primary purpose of this file is to define a dummy CLI command (`cwd`) used during testing. When executed, it fetches the current working directory of the running process via `System.IO.Directory.GetCurrentDirectory()` and writes it out asynchronously to the provided `IConsole` abstraction.

---

## Dependencies & Imports

- **`System.IO`**: Provides `Directory.GetCurrentDirectory()` to retrieve the current physical execution path.
- **`System.Threading.Tasks`**: Provides the `ValueTask` return type for asynchronous operations.
- **`CliFx`**: Core namespace providing the `[Command]` attribute and `ICommand` interface.
- **`CliFx.Binding`**: Namespace for binding attributes and CliFx execution pipeline infrastructure.
- **`CliFx.Infrastructure`**: Contains the `IConsole` interface for abstracting console input/output streams.

---

## Class Specification

### Attributes
- **`[Command("cwd")]`**
  Decorates `WorkingDirectoryCommand` as a runnable command in `CliFx`. It maps the command-line identifier `cwd` to this specific class.

### Class Definition
```csharp
public partial class WorkingDirectoryCommand : ICommand
```
- **Modifiers**: `public partial`
- **Interfaces**: Implements `CliFx.ICommand`, requiring the implementation of the `ExecuteAsync(IConsole console)` method.

---

## Methods

### `ExecuteAsync`

```csharp
public async ValueTask ExecuteAsync(IConsole console)
```

#### Description
Executes the command logic asynchronously when invoked by the CliFx framework.

#### Parameters
| Parameter | Type | Description |
| :--- | :--- | :--- |
| `console` | `IConsole` | The abstraction over standard console input, output, and error streams provided by CliFx. |

#### Return Value
- **`ValueTask`**: Represents the asynchronous execution completion of the command.

#### Implementation Details
1. Calls `Directory.GetCurrentDirectory()` to get the absolute path of the current working directory.
2. Calls `console.Output.WriteLineAsync(...)` to write the path to the standard output stream asynchronously.
3. Awaits the write operation and completes.

---

## Execution Flow

1. The CLI framework triggers `WorkingDirectoryCommand` via the `cwd` command name.
2. CliFx passes an instance of `IConsole` to `ExecuteAsync`.
3. `ExecuteAsync` retrieves the current working directory string from `Directory.GetCurrentDirectory()`.
4. The path is written to `console.Output` using `WriteLineAsync`.
5. The method completes its `ValueTask` execution.