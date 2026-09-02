# Technical Documentation: `BufferedCommandResult`

**File:** `CliWrap/Buffered/BufferedCommandResult.cs`  
**Namespace:** `CliWrap.Buffered`

---

## Overview

The `BufferedCommandResult` class represents the result of executing a command whose standard output (`stdout`) and standard error (`stderr`) streams have been buffered into memory as text strings. 

It extends the base `CommandResult` class to include the collected output strings in addition to execution metrics (exit code, start time, and exit time).

---

## Class Signature & Hierarchy

```csharp
namespace CliWrap.Buffered;

public partial class BufferedCommandResult(
    int exitCode,
    DateTimeOffset startTime,
    DateTimeOffset exitTime,
    string standardOutput,
    string standardError
) : CommandResult(exitCode, startTime, exitTime)
```

- **Inheritance:** `BufferedCommandResult` $\rightarrow$ `CommandResult`
- **Modifiers:** `public partial`
- **Constructor:** Uses C# primary constructor syntax.

---

## Constructor Parameters

The primary constructor accepts five parameters:

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `exitCode` | `int` | The exit code returned by the executed process (passed to base `CommandResult`). |
| `startTime` | `DateTimeOffset` | The timestamp indicating when the command execution started (passed to base `CommandResult`). |
| `exitTime` | `DateTimeOffset` | The timestamp indicating when the command execution completed (passed to base `CommandResult`). |
| `standardOutput` | `string` | The complete text captured from the standard output stream (`stdout`). |
| `standardError` | `string` | The complete text captured from the standard error stream (`stderr`). |

---

## Properties

### `StandardOutput`
```csharp
public string StandardOutput { get; }
```
* **Type:** `string`
* **Access:** Read-only (`get`)
* **Description:** Contains the buffered standard output data (`stdout`) produced by the process during execution.

---

### `StandardError`
```csharp
public string StandardError { get; }
```
* **Type:** `string`
* **Access:** Read-only (`get`)
* **Description:** Contains the buffered standard error data (`stderr`) produced by the process during execution.

---

### Inherited Properties (from `CommandResult`)
Through its base class `CommandResult`, `BufferedCommandResult` exposes:
* `ExitCode` (`int`)
* `StartTime` (`DateTimeOffset`)
* `ExitTime` (`DateTimeOffset`)

---

## Methods

### `Deconstruct`
```csharp
public void Deconstruct(out int exitCode, out string standardOutput, out string standardError)
```
Deconstructs the `BufferedCommandResult` instance into its core components. This enables C# tuple-like deconstruction syntax.

* **Output Parameters:**
  * `out int exitCode`: Populated with the value of `ExitCode`.
  * `out string standardOutput`: Populated with the value of `StandardOutput`.
  * `out string standardError`: Populated with the value of `StandardError`.

---

## Operators & Conversions

### Implicit Conversion to `string`
```csharp
public static implicit operator string(BufferedCommandResult result) => result.StandardOutput;
```
Allows a `BufferedCommandResult` object to be implicitly cast to a `string`. When converted, it returns the value stored in the `StandardOutput` property.

---

## Code Examples

### Standard Usage & Property Access
```csharp
// Accessing buffered properties
int exitCode = result.ExitCode;
string output = result.StandardOutput;
string error = result.StandardError;
```

### Deconstruction
```csharp
// Deconstruct the result into individual variables
var (exitCode, stdout, stderr) = bufferedResult;
```

### Implicit Conversion
```csharp
// Implicitly convert the result directly to a string (extracts StandardOutput)
string stdoutText = bufferedResult;
```