# Technical Documentation: `CliWrap/CommandResult.cs`

## Overview

The `CommandResult` class in the `CliWrap` namespace encapsulates the final outcome of executing a command line command. It provides details regarding the process termination status, execution timestamps, total execution duration, and implicit conversions for simplified evaluation.

---

## Class Declaration

```csharp
namespace CliWrap;

public partial class CommandResult(int exitCode, DateTimeOffset startTime, DateTimeOffset exitTime)
```

- **Namespace**: `CliWrap`
- **Modifiers**: `public partial`
- **Primary Constructor**: Accepts three parameters:
  - `exitCode` (`int`): The numerical exit code returned by the process.
  - `startTime` (`DateTimeOffset`): The timestamp indicating when the command execution began.
  - `exitTime` (`DateTimeOffset`): The timestamp indicating when the command execution completed.

---

## Properties

| Property | Type | Access | Description |
| :--- | :--- | :--- | :--- |
| `ExitCode` | `int` | `get` | Gets the exit code returned by the underlying process upon termination. Initialized via the constructor. |
| `IsSuccess` | `bool` | `get` | Evaluates whether the execution was successful. Returns `true` if `ExitCode` equals `0`; otherwise, `false`. |
| `StartTime` | `DateTimeOffset` | `get` | Gets the precise time at which the command started executing. Initialized via the constructor. |
| `ExitTime` | `DateTimeOffset` | `get` | Gets the precise time at which the command completed execution. Initialized via the constructor. |
| `RunTime` | `TimeSpan` | `get` | Computes the total execution duration by calculating `ExitTime - StartTime`. |

---

## Implicit Operators

The class extends functionality by defining two implicit conversion operators in a second `partial` class block.

### 1. Implicit Conversion to `int`
```csharp
public static implicit operator int(CommandResult result) => result.ExitCode;
```
- **Purpose**: Enables a `CommandResult` instance to be implicitly cast to an integer (`int`).
- **Behavior**: Evaluates directly to the value of the `ExitCode` property.

### 2. Implicit Conversion to `bool`
```csharp
public static implicit operator bool(CommandResult result) => result.IsSuccess;
```
- **Purpose**: Enables a `CommandResult` instance to be implicitly cast to a boolean (`bool`).
- **Behavior**: Evaluates directly to the value of the `IsSuccess` property (i.e., whether `ExitCode == 0`).

---

## Execution Logic & Computations

1. **Initialization**: Instances are constructed using the primary constructor, pinning immutable values for `ExitCode`, `StartTime`, and `ExitTime`.
2. **Success Evaluation**: `IsSuccess` performs a direct check (`ExitCode == 0`) without storing state.
3. **Runtime Calculation**: `RunTime` dynamically computes the difference between `ExitTime` and `StartTime` when requested.
4. **Convenience Casting**: The explicit partial block allows standard type comparisons or assignments directly against boolean conditions or integer variables.