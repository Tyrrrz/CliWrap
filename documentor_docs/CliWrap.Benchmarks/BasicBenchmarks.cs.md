# Technical Documentation: `CliWrap.Benchmarks/BasicBenchmarks.cs`

## Overview

The `BasicBenchmarks.cs` file contains a set of performance benchmarks designed to evaluate and compare the basic execution performance and memory usage of **CliWrap** against two other popular .NET process execution libraries: **RunProcessAsTask** and **MedallionShell**.

The benchmark suite is implemented using the [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) framework.

---

## Class Attributes

The `BasicBenchmarks` class is decorated with two BenchmarkDotNet attributes:

1. **`[MemoryDiagnoser]`**
   * Configures BenchmarkDotNet to measure memory allocation statistics (such as allocated bytes and garbage collection collections) during execution.

2. **`[Orderer(SummaryOrderPolicy.FastestToSlowest)]`**
   * Orders the benchmark results in the generated summary report from the fastest execution time to the slowest.

---

## Benchmark Methods

All benchmark methods execute a dummy target program specified by `Tests.Dummy.Program.FilePath` asynchronously and return the resulting process exit code (`int`).

### 1. `CliWrap()`
* **Attributes**: `[Benchmark(Baseline = true)]`
* **Baseline**: Marked as the benchmark baseline. Other benchmarks in the run will be evaluated relative to `CliWrap`'s performance.
* **Logic**:
  ```csharp
  var result = await Cli.Wrap(Tests.Dummy.Program.FilePath).ExecuteAsync();
  return result.ExitCode;
  ```
* **Description**: Spawns and awaits the process using `CliWrap.Cli.Wrap(...)` and returns the `ExitCode`.

### 2. `RunProcessAsTask()`
* **Attributes**: `[Benchmark]`
* **Logic**:
  ```csharp
  var result = await ProcessEx.RunAsync(Tests.Dummy.Program.FilePath);
  return result.ExitCode;
  ```
* **Description**: Executes the process using the `RunProcessAsTask` library's `ProcessEx.RunAsync(...)` method and returns the `ExitCode`.

### 3. `MedallionShell()`
* **Attributes**: `[Benchmark]`
* **Logic**:
  ```csharp
  var result = await Medallion.Shell.Command.Run(Tests.Dummy.Program.FilePath).Task;
  return result.ExitCode;
  ```
* **Description**: Executes the process using `Medallion.Shell.Command.Run(...)` and awaits its underlying `Task`, returning the `ExitCode`.

---

## How It Works

1. **Target Application**: Each benchmark method calls an identical target executable specified by `Tests.Dummy.Program.FilePath`.
2. **Execution**: The underlying process runner for each library launches the executable asynchronously.
3. **Completion & Exit Code**: Each library awaits process completion and retrieves the exit code.
4. **Benchmarking**: BenchmarkDotNet runs these methods across multiple iterations to collect timing metrics and memory diagnostics, using `CliWrap()` as the comparison baseline and ordering results from fastest to slowest.

---

## Dependencies

* **`System.Threading.Tasks`**: Provides `Task<T>` for asynchronous operations.
* **`BenchmarkDotNet.Attributes`**: Provides `[MemoryDiagnoser]` and `[Benchmark]`.
* **`BenchmarkDotNet.Order`**: Provides `SummaryOrderPolicy` for ordering results.
* **`RunProcessAsTask`**: External library providing `ProcessEx`.
* **`Medallion.Shell`**: External library providing `Medallion.Shell.Command`.
* **`Tests.Dummy`**: Internal namespace providing `Program.FilePath` to the executable being run.