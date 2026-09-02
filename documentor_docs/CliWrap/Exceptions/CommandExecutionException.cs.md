# Developer Documentation: `CliWrap/Exceptions/CommandExecutionException.cs`

## Overview

The `CommandExecutionException` class is a specialized exception type in the `CliWrap.Exceptions` namespace. It is thrown when a command executed via CliWrap fails to complete successfully (for example, when an executed process returns a non-zero exit code or encounters an execution failure).

It derives from `CliWrapException` and encapsulates details about the failed execution, specifically the command's configuration, the exit code, an error message, and an optional inner exception.

---

## Class Declaration

```csharp
namespace CliWrap.Exceptions;

public class CommandExecutionException(
    ICommandConfiguration command,
    int exitCode,
    string message,
    Exception? innerException = null
) : CliWrapException(message, innerException)
```

- **Namespace:** `CliWrap.Exceptions`
- **Inheritance:** `CommandExecutionException` $\rightarrow$ `CliWrapException` $\rightarrow$ `System.Exception`

---

## Properties

| Property | Type | Accessors | Description |
| :--- | :--- | :--- | :--- |
| `Command` | `ICommandConfiguration` | `{ get; }` | Gets the command configuration (`ICommandConfiguration`) that triggered the exception. |
| `ExitCode` | `int` | `{ get; }` | Gets the numerical exit code returned by the process upon termination. |

*Note: Properties inherited from `CliWrapException` / `System.Exception` such as `Message` and `InnerException` are populated via the base constructor calls.*

---

## Constructors

### 1. Primary Constructor

```csharp
public CommandExecutionException(
    ICommandConfiguration command,
    int exitCode,
    string message,
    Exception? innerException = null
) : CliWrapException(message, innerException)
```

#### Parameters:
- `command` (`ICommandConfiguration`): The configuration of the command that failed.
- `exitCode` (`int`): The exit code produced by the execution.
- `message` (`string`): A human-readable description of the execution failure.
- `innerException` (`Exception?`, optional): The underlying exception that caused the current exception, if applicable. Defaults to `null`.

#### Behavior:
- Initializes the `Command` and `ExitCode` properties using C# primary constructor parameter capture.
- Invokes the base `CliWrapException` constructor passing `message` and `innerException`.

---

### 2. Overloaded Constructor (Legacy / Backward Compatibility)

```csharp
[ExcludeFromCodeCoverage]
public CommandExecutionException(
    ICommandConfiguration command, 
    int exitCode, 
    string message
) : this(command, exitCode, message, null) { }
```

#### Parameters:
- `command` (`ICommandConfiguration`): The configuration of the command that failed.
- `exitCode` (`int`): The exit code produced by the execution.
- `message` (`string`): A human-readable description of the execution failure.

#### Attributes & Notes:
- **`[ExcludeFromCodeCoverage]`**: Marked to be excluded from code coverage metrics.
- **Delegation**: Delegates directly to the primary constructor with `innerException` set to `null`.
- **Planned Deprecation**: Contains an inline code comment (`// TODO: (breaking change) remove in favor of an optional parameter in the constructor above`) indicating this overload will be removed in a future major version release.

---

## Code Summary & Flow

1. **Instantiation**: When a command execution fails, `CommandExecutionException` is instantiated using either the primary constructor or the 3-parameter overload.
2. **Base Propagation**: The `message` and `innerException` parameters are passed up to `CliWrapException` (and subsequently `System.Exception`).
3. **Context Preservation**: The exception instance retains immutable access to the `Command` configuration and the process's `ExitCode` via public getter properties.