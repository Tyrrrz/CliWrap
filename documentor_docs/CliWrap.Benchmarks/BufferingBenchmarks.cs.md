# Technical Documentation: `BufferingBenchmarks.cs`

## Overview

The `BufferingBenchmarks.cs` file contains a performance benchmark suite designed to compare the execution speed and memory overhead of **CliWrap** against other popular .NET process execution libraries when capturing/buffering standard output (`stdout`) and standard error (`stderr`).

It uses the **BenchmarkDotNet** framework to run controlled performance tests against a dummy executable (`Tests.Dummy.Program.FilePath`) tasked with generating text output (`generate text --length 100000000 --lines 1000`).

---

## File Details

* **File Path:** `CliWrap.Benchmarks/BufferingBenchmarks.cs`
* **Namespace:** `CliWrap.Benchmarks`

---

## Class Attributes

The `BufferingBenchmarks` class is decorated with two BenchmarkDotNet attributes:

| Attribute | Purpose |
| :--- | :--- |
| `[MemoryDiagnoser]` | Measures and reports memory allocations (bytes allocated and garbage collection counts) during benchmark execution. |
| `[Orderer(SummaryOrderPolicy.FastestToSlowest)]` | Configures the output summary table to display results sorted by execution time from fastest to slowest. |

---

## Benchmarked Target Operation

Every benchmark method executes the same target process and command:
* **Executable:** `Tests.Dummy.Program.FilePath`
* **Arguments:** `generate text --length 100000000 --lines 1000`
* **Return Value:** A tuple `(string, string)` containing `(StandardOutput, StandardError)` captured from the process.

---

## Benchmark Methods

### 1. `CliWrap()`
* **Attribute:** `[Benchmark(Baseline = true)]`
* **Return Type:** `Task<(string, string)>`
* **Description:** Acts as the baseline measurement for performance comparisons. Uses `CliWrap`'s builder syntax and buffered execution extension method (`ExecuteBufferedAsync`).
* **Implementation:**
  ```csharp
  var result = await Cli.Wrap(Tests.Dummy.Program.FilePath)
      .WithArguments(["generate", "text", "--length", "100000000", "--lines", "1000"])
      .ExecuteBufferedAsync();

  return (result.StandardOutput, result.StandardError);
  ```

---

### 2. `RunProcessAsTask()`
* **Attribute:** `[Benchmark]`
* **Return Type:** `Task<(string, string)>`
* **Description:** Evaluates process buffering using the `RunProcessAsTask` library (`ProcessEx.RunAsync`).
* **Implementation:**
  ```csharp
  var result = await ProcessEx.RunAsync(
      Tests.Dummy.Program.FilePath,
      "generate text --length 100000000 --lines 1000"
  );

  return (
      string.Join(Environment.NewLine, result.StandardOutput),
      string.Join(Environment.NewLine, result.StandardError)
  );
  ```
  *Note: `RunProcessAsTask` returns stdout and stderr as collections of lines, which are joined using `Environment.NewLine` to construct the final string tuple.*

---

### 3. `MedallionShell()`
* **Attribute:** `[Benchmark]`
* **Return Type:** `Task<(string, string)>`
* **Description:** Evaluates process buffering using the `MedallionShell` library (`Medallion.Shell.Shell.Default.Run`).
* **Implementation:**
  ```csharp
  var result = await Medallion
      .Shell.Shell.Default.Run(
          Tests.Dummy.Program.FilePath,
          ["generate", "text", "--length", "100000000", "--lines", "1000"]
      )
      .Task;

  return (result.StandardOutput, result.StandardError);
  ```

---

### 4. `ProcessX()`
* **Attribute:** `[Benchmark]`
* **Return Type:** `Task<(string, string)>`
* **Description:** Evaluates process buffering using Cysharp's `ProcessX` library (`Cysharp.Diagnostics.ProcessX.GetDualAsyncEnumerable`).
* **Implementation:**
  ```csharp
  var (_, stdOutStream, stdErrStream) = Cysharp.Diagnostics.ProcessX.GetDualAsyncEnumerable(
      Tests.Dummy.Program.FilePath,
      arguments: "generate text --length 100000000 --lines 1000"
  );

  var stdOutTask = stdOutStream.ToTask();
  var stdErrTask = stdErrStream.ToTask();

  await Task.WhenAll(stdOutTask, stdErrTask);

  return (
      string.Join(Environment.NewLine, stdOutTask.Result),
      string.Join(Environment.NewLine, stdErrTask.Result)
  );
  ```
  *Note: Consumes `stdOutStream` and `stdErrStream` asynchronously as tasks via `ToTask()`, awaits their completion with `Task.WhenAll`, and joins the line arrays using `Environment.NewLine`.*

---

## Dependencies

* **BenchmarkDotNet** (`BenchmarkDotNet.Attributes`, `BenchmarkDotNet.Order`)
* **CliWrap** (`CliWrap`, `CliWrap.Buffered`)
* **RunProcessAsTask** (`RunProcessAsTask`)
* **MedallionShell** (`Medallion.Shell`)
* **ProcessX** (`Cysharp.Diagnostics`)