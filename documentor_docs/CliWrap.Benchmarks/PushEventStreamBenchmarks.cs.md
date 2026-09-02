# Technical Documentation: `CliWrap.Benchmarks/PushEventStreamBenchmarks.cs`

## Overview

The `PushEventStreamBenchmarks.cs` file contains a benchmark suite designed to measure the execution performance and memory footprint of `CliWrap`'s push-based event streaming mechanism (`Observe()`). 

It leverages **BenchmarkDotNet** to perform benchmarks and **System.Reactive (Rx)** to consume events produced by an external process execution.

---

## Class Configuration & Attributes

```csharp
[MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PushEventStreamBenchmarks
```

### Decorators

* **`[MemoryDiagnoser]`**  
  Enables memory allocation tracking (heap allocations, GC collections) during benchmark execution.
* **`[Orderer(SummaryOrderPolicy.FastestToSlowest)]`**  
  Configures the benchmark summary report to display test results ordered from the fastest execution time to the slowest.

---

## Class Members

### `CliWrap()` Method

```csharp
[Benchmark(Baseline = true)]
public async Task<int> CliWrap()
```

#### Purpose
Executes a target executable (`Tests.Dummy.Program.FilePath`), subscribes to its push-based event stream via `.Observe()`, and counts the total number of stdout and stderr events produced by the process. This method serves as the standard baseline benchmark for comparison.

#### Parameters
* *None*

#### Return Value
* `Task<int>`: Asynchronously returns the total integer count of received `StandardOutputCommandEvent` and `StandardErrorCommandEvent` instances.

---

## Step-by-Step Execution Workflow

1. **Initialize Counter**:  
   Declares an integer `counter` set to `0` to keep track of target command events.

2. **Configure Command**:  
   Uses `Cli.Wrap(...)` pointing to `Tests.Dummy.Program.FilePath` and sets the argument list:
   * `"generate"`
   * `"text"`
   * `"--length"`
   * `"100000000"`
   * `"--lines"`
   * `"1000"`

3. **Stream via Reactive Extension (`Observe`)**:  
   Calls `.Observe()` to launch the command and return an `IObservable<CommandEvent>` stream.

4. **Consume Events (`ForEachAsync`)**:  
   Asynchronously processes each `cmdEvent` emitted by the stream using Rx's `ForEachAsync` extension method:
   * **`StandardOutputCommandEvent`**: Increments `counter` by 1.
   * **`StandardErrorCommandEvent`**: Increments `counter` by 1.
   * **Other Event Types**: Ignored.

5. **Completion**:  
   Awaits the completion of the stream and returns the accumulated `counter` value.

---

## Dependencies & Imports

| Namespace | Usage |
| :--- | :--- |
| `System.Reactive.Linq` | Provides the `.ForEachAsync()` extension method for reactive event processing. |
| `System.Threading.Tasks` | Provides `Task<T>` support for asynchronous execution. |
| `BenchmarkDotNet.Attributes` | Provides `[MemoryDiagnoser]` and `[Benchmark]` attributes. |
| `BenchmarkDotNet.Order` | Provides `[Orderer]` and `SummaryOrderPolicy`. |
| `CliWrap.EventStream` | Defines event models like `StandardOutputCommandEvent` and `StandardErrorCommandEvent`. |