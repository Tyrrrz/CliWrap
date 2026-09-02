# Technical Documentation: `ResourcePolicyBuilder.cs`

## Overview

The `ResourcePolicyBuilder` class is a builder utility within the `CliWrap.Builders` namespace. Its primary purpose is to provide a fluent interface for configuring resource allocation policies—such as process priority, CPU affinity, and memory working set limits—before constructing an immutable `ResourcePolicy` object.

## Class Definition

```csharp
namespace CliWrap.Builders;

public class ResourcePolicyBuilder
```

* **Namespace:** `CliWrap.Builders`
* **Dependencies:** `System.Diagnostics`

---

## Fields

The class maintains private internal state for four nullable resource configuration properties:

| Field | Type | Description |
| :--- | :--- | :--- |
| `_priority` | `ProcessPriorityClass?` | Stores the execution priority class for the process. |
| `_affinity` | `nint?` | Stores the processor core affinity mask for the process. |
| `_minWorkingSet` | `nint?` | Stores the minimum allowed working set (memory) size for the process. |
| `_maxWorkingSet` | `nint?` | Stores the maximum allowed working set (memory) size for the process. |

---

## Methods

All configuration methods return the `ResourcePolicyBuilder` instance (`this`), enabling method chaining.

### `SetPriority(ProcessPriorityClass? priority)`

Configures the process priority class (e.g., `Normal`, `High`, `Idle`).

* **Parameters:**
  * `priority` (`ProcessPriorityClass?`): The priority class to assign to the process, or `null` to leave unassigned.
* **Returns:** `ResourcePolicyBuilder` (the current builder instance).
* **Remarks:** Platform compatibility depends on `System.Diagnostics.Process.PriorityClass`.

---

### `SetAffinity(nint? affinity)`

Configures the CPU core affinity mask for the process.

* **Parameters:**
  * `affinity` (`nint?`): A bitmask representing the CPU cores on which the process is allowed to run (e.g., `0b1010` for cores 1 and 3 out of 4), or `null` to unset.
* **Returns:** `ResourcePolicyBuilder` (the current builder instance).
* **Remarks:** Platform compatibility depends on `System.Diagnostics.Process.ProcessorAffinity`.

---

### `SetMinWorkingSet(nint? minWorkingSet)`

Configures the minimum working set (physical memory allocation) for the process.

* **Parameters:**
  * `minWorkingSet` (`nint?`): The minimum working set size as a native integer pointer size, or `null` to leave unassigned.
* **Returns:** `ResourcePolicyBuilder` (the current builder instance).
* **Remarks:** Platform compatibility depends on `System.Diagnostics.Process.MinWorkingSet`.

---

### `SetMaxWorkingSet(nint? maxWorkingSet)`

Configures the maximum working set (physical memory allocation) for the process.

* **Parameters:**
  * `maxWorkingSet` (`nint?`): The maximum working set size as a native integer pointer size, or `null` to leave unassigned.
* **Returns:** `ResourcePolicyBuilder` (the current builder instance).
* **Remarks:** Platform compatibility depends on `System.Diagnostics.Process.MaxWorkingSet`.

---

### `Build()`

Instantiates and returns a `ResourcePolicy` populated with the current configuration state.

* **Parameters:** None.
* **Returns:** `ResourcePolicy` — A new instance initialized with `_priority`, `_affinity`, `_minWorkingSet`, and `_maxWorkingSet`.

```csharp
public ResourcePolicy Build() => new(_priority, _affinity, _minWorkingSet, _maxWorkingSet);
```

---

## Example Usage

```csharp
using System.Diagnostics;
using CliWrap.Builders;

var builder = new ResourcePolicyBuilder();

ResourcePolicy policy = builder
    .SetPriority(ProcessPriorityClass.High)
    .SetAffinity(0b0011) // Use CPU cores 0 and 1
    .SetMinWorkingSet(1024 * 1024 * 100) // 100 MB
    .SetMaxWorkingSet(1024 * 1024 * 500) // 500 MB
    .Build();
```