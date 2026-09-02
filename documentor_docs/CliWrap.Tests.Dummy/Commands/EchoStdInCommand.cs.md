# Documentation: `EchoStdInCommand.cs`

## Overview

The `EchoStdInCommand` class is a command implementation within the `CliWrap.Tests.Dummy` executable. It implements the `ICommand` interface provided by the `CliFx` framework.

Its primary purpose is to read binary data from the standard input (`stdin`) stream and output (echo) that data to configured target console output streams (`stdout`, `stderr`, or both) up to a specified maximum byte length.

---

## Command Syntax & Name

* **Command Name:** `echo stdin`
* **Class Name:** `EchoStdInCommand`
* **Namespace:** `CliWrap.Tests.Dummy.Commands`

---

## Command Options

The command exposes two configurable options via CLI flags:

| Option | Type | Default Value | Description |
| :--- | :--- | :--- | :--- |
| `--target` | `OutputTarget` | `OutputTarget.StdOut` | Specifies the destination output stream(s) where the read input bytes will be written. |
| `--length` | `long` | `long.MaxValue` | The maximum number of bytes to read from standard input before terminating the operation. |

---

## Method Breakdown

### `ValueTask ExecuteAsync(IConsole console)`

Executes the command logic asynchronously using the provided `CliFx.Infrastructure.IConsole` context.

#### Execution Flow

1. **Buffer Allocation:**
   * Rents an 81,920-byte (80 KB) buffer from `MemoryPool<byte>.Shared` using a `using` statement to ensure auto-disposal upon execution completion.

2. **Read Loop Initialization:**
   * Tracks total bytes read using `totalBytesRead` initialized to `0`.
   * Continues looping while `totalBytesRead` is strictly less than `Length`.

3. **Reading from Standard Input:**
   * Calculates `bytesWanted` as the minimum value between the buffer's capacity (`buffer.Memory.Length`) and the remaining bytes requested (`Length - totalBytesRead`).
   * Asynchronously reads data from `console.Input.BaseStream` into the buffer slice `buffer.Memory[..bytesWanted]`.
   * If `bytesRead` is `0` or negative (indicating End-Of-File / EOF), the loop terminates immediately.

4. **Writing to Target Output(s):**
   * Obtains the appropriate stream writer(s) using the extension method `console.GetWriters(Target)`.
   * Iterates through each writer and asynchronously writes the slice `buffer.Memory[..bytesRead]` directly to the writer's underlying `BaseStream`.

5. **Counter Update:**
   * Adds `bytesRead` to `totalBytesRead` and continues the loop until the byte length target is met or EOF is reached.

---

## Dependencies & Imports

* **`System` / `System.Buffers` / `System.Threading.Tasks`:** Provides core primitives, pooled memory mechanisms (`MemoryPool<byte>`), and async execution types (`ValueTask`).
* **`CliFx` / `CliFx.Binding` / `CliFx.Infrastructure`:** Command-line parsing framework interface (`ICommand`, `[Command]`, `[CommandOption]`, `IConsole`).
* **`CliWrap.Tests.Dummy.Commands.Shared`:** Contains shared helpers/enums used across dummy commands, such as `OutputTarget` and `console.GetWriters(...)`.