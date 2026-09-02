# Technical Documentation: `PipeToStreamBenchmarks.cs`

## Overview

The `PipeToStreamBenchmarks.cs` file contains a benchmark suite designed to measure and compare the performance and memory usage of piping process output into a stream using **CliWrap** versus **MedallionShell**.

It utilizes the **BenchmarkDotNet** library to execute benchmark runs, track memory allocations, and order results based on execution speed.

---

## File Details

- **Namespace:** `CliWrap.Benchmarks`
- **Class:** `PipeToStreamBenchmarks`
- **Dependencies:**
  - `System.IO`
  - `System.Threading.Tasks`
  - `BenchmarkDotNet.Attributes`
  - `BenchmarkDotNet.Order`

---

## Class Attributes

The `PipeToStreamBenchmarks` class is decorated with BenchmarkDotNet attributes to configure benchmark reporting and measurement:

| Attribute | Purpose |
| :--- | :--- |
| `[MemoryDiagnoser]` | Tracks and reports memory allocation metrics (GC allocations, heap usage) during execution. |
| `[Orderer(SummaryOrderPolicy.FastestToSlowest)]` | Orders the resulting benchmark summary table from the fastest execution time to the slowest. |

---

## Benchmark Methods

The class contains two benchmark methods that execute identical workloads—running an external executable (`Tests.Dummy.Program.FilePath`) with the arguments `["generate", "binary"]` and redirecting its output into a `MemoryStream`.

### 1. `CliWrap()`

* **ReturnType:** `Task<Stream>`
* **Attributes:** `[Benchmark(Baseline = true)]`

#### Description
Serves as the baseline benchmark for the test suite. It uses CliWrap to pipe process output into a stream.

#### Execution Flow
1. Initializes an asynchronous disposable `MemoryStream` (`await using var stream = new MemoryStream()`).
2. Configures a process invocation using `Cli.Wrap(...)` targeting `Tests.Dummy.Program.FilePath`.
3. Sets arguments using `.WithArguments(["generate", "binary"])`.
4. Connects the command output to the `MemoryStream` using CliWrap's pipe operator (`| stream`).
5. Asynchronously executes the command via `await command.ExecuteAsync()`.
6. Returns the target stream.

---

### 2. `MedallionShell()`

* **ReturnType:** `Task<Stream>`
* **Attributes:** `[Benchmark]`

#### Description
Compares the performance of the MedallionShell library against the CliWrap baseline for the same piping operation.

#### Execution Flow
1. Initializes an asynchronous disposable `MemoryStream` (`await using var stream = new MemoryStream()`).
2. Starts the command via `Medallion.Shell.Command.Run(...)` with `Tests.Dummy.Program.FilePath` and arguments `["generate", "binary"]`.
3. Redirects the output to the `MemoryStream` using MedallionShell's output operator (`> stream`).
4. Awaits process completion via `await command.Task`.
5. Returns the target stream.

---

## Summary Comparison Matrix

| Benchmark Method | Execution Model | Piping Operator | Async Await Strategy | Baseline |
| :--- | :--- | :--- | :--- | :--- |
| `CliWrap()` | `Cli.Wrap(...).ExecuteAsync()` | `\|` | `await command.ExecuteAsync()` | **Yes** |
| `MedallionShell()` | `Medallion.Shell.Command.Run(...)` | `>` | `await command.Task` | No |