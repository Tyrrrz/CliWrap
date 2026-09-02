# Technical Documentation: `LengthStdInCommand.cs`

## Overview

The `LengthStdInCommand` class is a dummy CLI command implemented within the `CliWrap.Tests.Dummy` test project using the **CliFx** framework. Its primary purpose is to asynchronously read all data provided via standard input (`stdin`), calculate the total number of bytes read, and print that count to standard output (`stdout`).

This class is used primarily for integration and unit testing within the CliWrap suite to verify standard input piping and streaming functionality.

---

## File Information

* **File Path:** `CliWrap.Tests.Dummy/Commands/LengthStdInCommand.cs`
* **Namespace:** `CliWrap.Tests.Dummy.Commands`
* **Command Route:** `length stdin`

---

## Class Definition & Interfaces

```csharp
[Command("length stdin")]
public partial class LengthStdInCommand : ICommand
```

* **Attributes:**
  * `[Command("length stdin")]`: Registers this class as a CliFx executable command mapped to the `length stdin` CLI argument pattern.
* **Interfaces:**
  * `ICommand`: The standard CliFx interface requiring the implementation of the `ExecuteAsync(IConsole console)` method.

---

## Code Breakdown

### `ExecuteAsync(IConsole console)`

The entry point for the command execution, receiving an instance of `IConsole` from the CliFx framework.

```csharp
public async ValueTask ExecuteAsync(IConsole console)
{
    using var buffer = MemoryPool<byte>.Shared.Rent(81920);

    var totalBytesRead = 0L;
    while (true)
    {
        var bytesRead = await console.Input.BaseStream.ReadAsync(buffer.Memory);
        if (bytesRead <= 0)
            break;

        totalBytesRead += bytesRead;
    }

    await console.Output.WriteLineAsync(totalBytesRead.ToString(CultureInfo.InvariantCulture));
}
```

#### Step-by-Step Execution Flow

1. **Buffer Allocation:**
   * Rents a byte buffer of size 81,920 bytes (80 KB) from the shared memory pool (`MemoryPool<byte>.Shared.Rent(81920)`).
   * The `using` declaration ensures the rented memory buffer is returned to the pool once execution completes.

2. **Stream Reading Loop:**
   * Tracks accumulated bytes using `totalBytesRead` initialized to `0L` (64-bit integer).
   * Continuously calls `await console.Input.BaseStream.ReadAsync(buffer.Memory)` to pull data chunks from standard input (`stdin`).
   * **Termination Condition:** If `bytesRead` returns `0` or a negative value (indicating End-of-Stream / EOF), the `while` loop terminates.
   * **Accumulation:** For each read operation returning a value greater than `0`, `bytesRead` is added to `totalBytesRead`.

3. **Output Generation:**
   * Converts `totalBytesRead` to a string formatted using invariant culture (`CultureInfo.InvariantCulture`).
   * Asynchronously writes the resulting byte count string followed by a line terminator to standard output (`console.Output.WriteLineAsync`).

---

## Key Dependencies

| Dependency | Purpose |
| :--- | :--- |
| `CliFx` | CLI framework providing the `[Command]` attribute and `ICommand` interface. |
| `CliFx.Infrastructure` | Provides the `IConsole` abstraction for accessing `Input` and `Output` streams. |
| `System.Buffers` | Supplies `MemoryPool<byte>` for efficient, unmanaged memory buffer management. |
| `System.Globalization` | Supplies `CultureInfo.InvariantCulture` for consistent numeric string formatting. |
| `System.Threading.Tasks` | Supports `ValueTask` and `async`/`await` asynchronous execution patterns. |