# Technical Documentation: `PipeFromStreamBenchmarks.cs`

## Overview

The `PipeFromStreamBenchmarks` class contains performance benchmarks designed to compare the execution speed and memory usage of piping input from an existing `Stream` (specifically a `MemoryStream`) to an external process using **CliWrap** versus **MedallionShell**. 

It uses the [BenchmarkDotNet](https://benchmarkdotnet.org/) library to run performance tests and collect diagnosable metrics.

---

## File Details

- **File Path:** `CliWrap.Benchmarks/PipeFromStreamBenchmarks.cs`
- **Namespace:** `CliWrap.Benchmarks`
- **Class:** `PipeFromStreamBenchmarks`

---

## Class Attributes

- `[MemoryDiagnoser]`: Enables memory allocation and garbage collection diagnostics in BenchmarkDotNet output reports.
- `[Orderer(SummaryOrderPolicy.FastestToSlowest)]`: Configures the final benchmark summary report to display test results ordered from the fastest execution time to the slowest.

---

## Benchmarks

### 1. `CliWrap()`

- **Attributes:** `[Benchmark(Baseline = true)]`
  - Designates this method as the baseline against which other benchmark results are compared.
- **Return Type:** `Task<Stream>`
- **Description:** Measures performance when redirecting standard input from a stream using CliWrap.

#### Workflow:
1. Instantiates a `MemoryStream` populated with the byte array `[1, 2, 3, 4, 5]` inside an `await using` scope for proper asynchronous disposal.
2. Constructs a CliWrap command targeting the dummy program located at `Tests.Dummy.Program.FilePath` with arguments `["echo", "stdin"]`.
3. Pipes the input `stream` into the command using CliWrap's `|` operator overloading syntax (`stream | Cli.Wrap(...)`).
4. Executes the command asynchronously via `await command.ExecuteAsync()`.
5. Returns the `stream`.

---

### 2. `MedallionShell()`

- **Attributes:** `[Benchmark]`
- **Return Type:** `Task<Stream>`
- **Description:** Measures performance when redirecting standard input from a stream using the MedallionShell library.

#### Workflow:
1. Instantiates a `MemoryStream` populated with the byte array `[1, 2, 3, 4, 5]` inside an `await using` scope for proper asynchronous disposal.
2. Executes a process via `Medallion.Shell.Command.Run` targeting `Tests.Dummy.Program.FilePath` with arguments `["echo", "stdin"]`.
3. Pipes the input `stream` into the standard input using MedallionShell's `<` operator syntax (`Command.Run(...) < stream`).
4. Awaits completion of the command via `await command.Task`.
5. Returns the `stream`.

---

## Key Comparisons Summary

| Feature / Metric | `CliWrap()` | `MedallionShell()` |
| :--- | :--- | :--- |
| **Benchmark Role** | Baseline | Comparison Target |
| **Input Source** | `MemoryStream` (`[1, 2, 3, 4, 5]`) | `MemoryStream` (`[1, 2, 3, 4, 5]`) |
| **Piping Syntax** | `stream \| Cli.Wrap(...)` | `Command.Run(...) < stream` |
| **Execution Method** | `await command.ExecuteAsync()` | `await command.Task` |