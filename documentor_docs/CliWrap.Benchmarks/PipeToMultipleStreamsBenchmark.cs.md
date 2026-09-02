# Technical Documentation: `PipeToMultipleStreamsBenchmark.cs`

## Overview

The `PipeToMultipleStreamsBenchmark.cs` file contains performance benchmarking logic designed to measure the execution time and memory allocations of piping process stdout/stderr to multiple destination streams simultaneously using the **CliWrap** library.

It utilizes the [BenchmarkDotNet](https://benchmarkdotnet.org/) framework to collect runtime statistics and memory diagnostics.

---

## File Details

* **File Path:** `CliWrap.Benchmarks/PipeToMultipleStreamsBenchmark.cs`
* **Namespace:** `CliWrap.Benchmarks`

---

## Class: `PipeToMultipleStreamsBenchmark`

### Attributes

| Attribute | Description |
| :--- | :--- |
| `[MemoryDiagnoser]` | Instructs BenchmarkDotNet to capture memory allocation metrics (GC collections, bytes allocated) during the run. |
| `[Orderer(SummaryOrderPolicy.FastestToSlowest)]` | Orders the benchmark summary results sorted from the fastest to the slowest execution duration. |

---

## Benchmark Methods

### `CliWrap()`

* **Attribute:** `[Benchmark(Baseline = true)]`
* **Return Type:** `Task<(Stream, Stream)>`

#### Purpose
Measures the performance of using CliWrap to execute an external executable and direct its standard output to two `MemoryStream` instances in parallel via a merged pipe target. This method serves as the baseline measurement for the benchmark suite.

#### Internal Execution Flow

1. **Stream Instantiation:**
   Creates two `MemoryStream` instances (`stream1` and `stream2`) using asynchronous disposable scope (`await using`).

2. **Pipe Target Merging:**
   Creates a merged `PipeTarget` using `PipeTarget.Merge(...)`, combining:
   * `PipeTarget.ToStream(stream1)`
   * `PipeTarget.ToStream(stream2)`

3. **Command Construction:**
   Constructs a CliWrap command targeting `Tests.Dummy.Program.FilePath`:
   * Passes the arguments `["generate", "binary"]` via `.WithArguments(...)`.
   * Pipes the standard output of the command into `target` using the custom `|` operator overload.

4. **Execution:**
   Executes the configured command asynchronously using `await command.ExecuteAsync()`.

5. **Return Value:**
   Returns a tuple containing `(stream1, stream2)`.

---

## Code Breakdown Summary

```csharp
namespace CliWrap.Benchmarks;

[MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PipeToMultipleStreamsBenchmark
{
    [Benchmark(Baseline = true)]
    public async Task<(Stream, Stream)> CliWrap()
    {
        // 1. Initialize destination streams
        await using var stream1 = new MemoryStream();
        await using var stream2 = new MemoryStream();

        // 2. Merge targets so output is duplicated into both streams
        var target = PipeTarget.Merge(PipeTarget.ToStream(stream1), PipeTarget.ToStream(stream2));

        // 3. Configure command and pipe output to the merged target
        var command =
            Cli.Wrap(Tests.Dummy.Program.FilePath).WithArguments(["generate", "binary"]) | target;

        // 4. Asynchronously execute the command
        await command.ExecuteAsync();

        // 5. Return streams
        return (stream1, stream2);
    }
}
```