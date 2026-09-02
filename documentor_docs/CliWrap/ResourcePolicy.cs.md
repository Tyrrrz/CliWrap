# Technical Documentation: `CliWrap/ResourcePolicy.cs`

## Overview

The `ResourcePolicy` class in the `CliWrap` namespace defines a configuration object used to specify OS-level resource limits and process control settings (such as process priority, CPU core affinity, and memory working set sizes) for a target process.

## Class Definition

**Namespace:** `CliWrap`  
**Assembly:** CliWrap  
**Type:** `public partial class ResourcePolicy`

```csharp
public partial class ResourcePolicy(
    ProcessPriorityClass? priority = null,
    nint? affinity = null,
    nint? minWorkingSet = null,
    nint? maxWorkingSet = null
)
```

## Purpose

The `ResourcePolicy` class encapsulates configuration settings that correspond to standard process management parameters found in the .NET `System.Diagnostics.Process` API. All properties are optional and nullable (`null` indicates that no explicit policy override is requested, relying on system defaults).

## Members

### Constructor

#### Primary Constructor
```csharp
public ResourcePolicy(
    ProcessPriorityClass? priority = null,
    nint? affinity = null,
    nint? minWorkingSet = null,
    nint? maxWorkingSet = null
)
```
Initializes a new instance of the `ResourcePolicy` class using optional parameters.

* **Parameters:**
  * `priority` (`ProcessPriorityClass?`): Sets the initial value for `Priority`. Default is `null`.
  * `affinity` (`nint?`): Sets the initial value for `Affinity`. Default is `null`.
  * `minWorkingSet` (`nint?`): Sets the initial value for `MinWorkingSet`. Default is `null`.
  * `maxWorkingSet` (`nint?`): Sets the initial value for `MaxWorkingSet`. Default is `null`.

---

### Static Properties

#### `Default`
```csharp
public static ResourcePolicy Default { get; } = new();
```
* **Type:** `ResourcePolicy`
* **Description:** Provides a singleton instance representing the default resource policy where all underlying resource settings (`Priority`, `Affinity`, `MinWorkingSet`, `MaxWorkingSet`) are set to `null`.

---

### Instance Properties

#### `Priority`
```csharp
public ProcessPriorityClass? Priority { get; }
```
* **Type:** `System.Diagnostics.ProcessPriorityClass?`
* **Access:** Read-only (`get`)
* **Description:** Represents the priority class assigned to the process (e.g., `Normal`, `High`, `Idle`).

#### `Affinity`
```csharp
public nint? Affinity { get; }
```
* **Type:** `nint?` (Native Integer)
* **Access:** Read-only (`get`)
* **Description:** Defines the bitmask representing the CPU cores that the process is allowed to execute on.

#### `MinWorkingSet`
```csharp
public nint? MinWorkingSet { get; }
```
* **Type:** `nint?` (Native Integer)
* **Access:** Read-only (`get`)
* **Description:** Specifies the minimum allowable working set size (RAM allocation) for the process.

#### `MaxWorkingSet`
```csharp
public nint? MaxWorkingSet { get; }
```
* **Type:** `nint?` (Native Integer)
* **Access:** Read-only (`get`)
* **Description:** Specifies the maximum allowable working set size (RAM allocation) for the process.

---

## Platform Support Considerations

As noted in the source documentation, platform support and behavior for these settings depend on the underlying implementation of the standard .NET `System.Diagnostics.Process` class attributes:
* `Process.PriorityClass`
* `Process.ProcessorAffinity`
* `Process.MinWorkingSet`
* `Process.MaxWorkingSet`

*Note: Certain operating systems or execution environments may restrict or ignore specific settings (such as working set limits or affinity masks).*