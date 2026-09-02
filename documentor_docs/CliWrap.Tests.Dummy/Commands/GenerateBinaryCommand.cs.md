# Technical Documentation: `GenerateBinaryCommand.cs`

**File Location:** `CliWrap.Tests.Dummy/Commands/GenerateBinaryCommand.cs`  
**Namespace:** `CliWrap.Tests.Dummy.Commands`  

---

## 1. Overview

The `GenerateBinaryCommand` class is a command-line interface (CLI) command implemented using the **CliFx** framework. Its primary purpose is to generate pseudo-random binary data of a specified length and stream it to a designated console output stream (such as standard output or standard error).

To ensure test repeatability, the class generates random data using a fixed seed.

---

## 2. Command Metadata

- **Command Name:** `generate binary`
- **Interface Implemented:** `CliFx.ICommand`

To invoke this command via the CLI, the route is `generate binary`.

---

## 3. Class Members & Options

### Fields

* **`private readonly Random _random = new(1234567);`**
  * An instance of `System.Random` initialized with a fixed seed (`1234567`). 
  * Ensures that generated binary data is deterministic across test executions.

### Command Options (Properties)

The command exposes three configurable options mapped via CliFx's `[CommandOption]` attribute:

| Property | Type | Default Value | Option Name | Description |
| :--- | :--- | :--- | :--- | :--- |
| **`Target`** | `OutputTarget` | `OutputTarget.StdOut` | `"target"` | Specifies the stream output target (e.g., stdout/stderr). |
| **`Length`** | `long` | `100_000` | `"length"` | Total number of binary bytes to generate. |
| **`BufferSize`** | `int` | `1024` | `"buffer"` | Size of the buffer in bytes used for chunked memory allocations. |

---

## 4. Execution Logic (`ExecuteAsync`)

The primary execution logic resides within `ExecuteAsync(IConsole console)`. 

### Method Signature
```csharp
public async ValueTask ExecuteAsync(IConsole console)
```

### Step-by-Step Workflow

1. **Buffer Allocation**:
   * Rents a `MemoryPool<byte>` chunk using `MemoryPool<byte>.Shared.Rent(BufferSize)`.
   * Wrapped in a `using` statement to guarantee proper disposal of memory pooled resources once the method completes.

2. **Generation & Streaming Loop**:
   * Tracks total bytes generated using `totalBytesGenerated` starting at `0`.
   * Loops continuously while `totalBytesGenerated < Length`:
     1. **Random Data Population**: Fills the memory span buffer (`buffer.Memory.Span`) with pseudo-random bytes via `_random.NextBytes(...)`.
     2. **Chunk Calculation**: Determines `bytesWanted` by taking the minimum of the rental buffer size and remaining target bytes (`Length - totalBytesGenerated`).
     3. **Asynchronous Stream Writing**:
        * Retrieves stream writers from `console.GetWriters(Target)`.
        * Asynchronously writes the byte slice `buffer.Memory[..bytesWanted]` directly to `writer.BaseStream`.
     4. **Progress Increment**: Adds `bytesWanted` to `totalBytesGenerated`.

---

## 5. Dependencies & Imports

* **`System`**: Core structures (`Math`, `Random`).
* **`System.Buffers`**: High-performance memory management (`MemoryPool<byte>`).
* **`System.Threading.Tasks`**: Asynchronous programming types (`ValueTask`).
* **`CliFx` / `CliFx.Binding` / `CliFx.Infrastructure`**: CLI framework attributes (`[Command]`, `[CommandOption]`), command interfaces (`ICommand`), and console abstractions (`IConsole`).
* **`CliWrap.Tests.Dummy.Commands.Shared`**: Contains shared infrastructure definitions such as `OutputTarget` and console extension methods (`GetWriters`).