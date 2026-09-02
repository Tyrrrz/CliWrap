# Technical Documentation: `CliWrap.Benchmarks/PullEventStreamBenchmarks.cs`

## Overview

The `PullEventStreamBenchmarks` class is a BenchmarkDotNet performance test suite designed to measure and compare the execution speed and memory allocation of `CliWrap`'s pull-based event streaming against an alternative C# library, `ProcessX` (`Cysharp.Diagnostics.ProcessX`).

The benchmarks execute a dummy executable (`Tests.Dummy.Program.FilePath`) that outputs standard output and error text streams, and measure how efficiently each library consumes these streams asynchronously.

---

## Class Attributes

- `[MemoryDiagnoser]`  
  Instructs BenchmarkDotNet to track and report memory allocations and Garbage Collection (GC) activity for each benchmark method.

- `[Orderer(SummaryOrderPolicy.FastestToSlowest)]`  
  Configures BenchmarkDotNet to order the benchmark results in the output summary from fastest execution time to slowest.

---

## Key Components & Benchmark Methods

### 1. `CliWrap()`

- **Attribute**: `[Benchmark(Baseline = true)]`
- **Return Type**: `Task<int>`
- **Description**: Acts as the baseline benchmark for comparison. It measures `CliWrap`'s event streaming capability via `ListenAsync()`.

#### Execution Steps:
1. Initializes an integer variable `counter` to `0`.
2. Creates a process execution definition using `Cli.Wrap(Tests.Dummy.Program.FilePath)`.
3. Configures command-line arguments: `["generate", "text", "--length", "100000000", "--lines", "1000"]`.
4. Calls `.ListenAsync()` to obtain an `IAsyncEnumerable<CommandEvent>` pull-based stream.
5. Asynchronously iterates through incoming command events using `await foreach`.
6. Uses pattern matching to check if the `cmdEvent` is either:
   - `StandardOutputCommandEvent`
   - `StandardErrorCommandEvent`
7. Increments `counter` for each matched standard output or standard error event.
8. Returns the total count of captured stdout and stderr events.

---

### 2. `ProcessX()`

- **Attribute**: `[Benchmark]`
- **Return Type**: `Task<int>`
- **Description**: Measures the performance of stream consumption using `Cysharp.Diagnostics.ProcessX`.

#### Execution Steps:
1. Initializes an integer variable `counter` to `0`.
2. Calls `Cysharp.Diagnostics.ProcessX.GetDualAsyncEnumerable(...)` targeting `Tests.Dummy.Program.FilePath` with the argument string `"generate text --length 100000000 --lines 1000"`.
3. Deconstructs the return tuple to obtain `stdOutStream` and `stdErrStream`.
4. Spawns two asynchronous background tasks via `Task.Run`:
   - `consumeStdOutTask`: Asynchronously iterates through `stdOutStream` using `await foreach` and increments `counter` for each item.
   - `consumeStdErrorTask`: Asynchronously iterates through `stdErrStream` using `await foreach` and increments `counter` for each item.
5. Awaits the completion of both tasks using `Task.WhenAll(consumeStdOutTask, consumeStdErrorTask)`.
6. Returns the final value of `counter`.

---

## Workflow Summary

```
 PullEventStreamBenchmarks
 ├── CliWrap() [Baseline Benchmark]
 │    ├── Spawns dummy process via CliWrap
 │    ├── Consumes stream with ListenAsync()
 │    └── Counts stdout & stderr events via pattern matching
 │
 └── ProcessX() [Benchmark]
      ├── Spawns dummy process via ProcessX GetDualAsyncEnumerable
      ├── Runs concurrent Task.Run loops for stdout and stderr streams
      └── Counts streamed items across both tasks
```