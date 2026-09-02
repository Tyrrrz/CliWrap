# Technical Documentation: `CliWrap.Benchmarks/Program.cs`

## Overview

The `Program.cs` file serves as the main entry point for the `CliWrap.Benchmarks` application. Its sole purpose is to initialize and execute performance benchmarks using the **BenchmarkDotNet** library across the current assembly.

---

## Code Breakdown

```csharp
using System.Reflection;
using BenchmarkDotNet.Running;

namespace CliWrap.Benchmarks;

public static class Program
{
    public static void Main() => BenchmarkRunner.Run(Assembly.GetExecutingAssembly());
}
```

---

## Dependencies and Namespaces

* **`System.Reflection`**: Provides access to assembly metadata. Used specifically to obtain a reference to the currently executing assembly via `Assembly.GetExecutingAssembly()`.
* **`BenchmarkDotNet.Running`**: Component of the BenchmarkDotNet framework providing the `BenchmarkRunner` class used to execute benchmarks.
* **`namespace CliWrap.Benchmarks`**: Defines the namespace context for the benchmark runner application using C# file-scoped namespace syntax.

---

## Key Components

### `public static class Program`
A standard C# static class that acts as the container for the application's entry point.

### `public static void Main()`
The entry point method executed when the console application launches. It uses an expression-bodied member (`=>`) to call the benchmark runner directly.

---

## How It Works

1. **Application Launch**: When the `CliWrap.Benchmarks` console application runs, execution begins at `Program.Main()`.
2. **Assembly Retrieval**: `Assembly.GetExecutingAssembly()` is invoked to obtain reflection metadata for the currently running assembly (`CliWrap.Benchmarks`).
3. **Benchmark Execution**: `BenchmarkRunner.Run(...)` takes the assembly reference, automatically scans it for classes and methods attributed as benchmarks, and executes the benchmark suite.