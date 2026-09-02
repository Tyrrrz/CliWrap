# Technical Documentation: `CliWrap/EventStream/CommandEvent.cs`

## Overview

The `CliWrap.EventStream` namespace provides a stream-based model for handling process execution events. The file `CommandEvent.cs` defines the hierarchy of event objects emitted during a process execution lifecycle. 

The core type, `CommandEvent`, is an abstract base class representing any event produced by a executed command. Consumers handle specific events using pattern matching on its concrete derived classes.

---

## Class Hierarchy Overview

```
CommandEvent (abstract base)
 ├── StartedCommandEvent
 ├── StandardOutputCommandEvent
 ├── StandardErrorCommandEvent
 └── ExitedCommandEvent
```

---

## Component Reference

### 1. `CommandEvent`

* **Type**: `public abstract class`
* **Description**: The base abstract class for all events emitted during the command execution lifecycle. It serves as the common polymorphic type for stream-based event handling.

---

### 2. `StartedCommandEvent`

* **Type**: `public class`
* **Base Class**: `CommandEvent`
* **Description**: Represents the event triggered when the underlying process starts executing. This event typically appears only once at the beginning of an event stream.

#### Constructors
* `StartedCommandEvent(int processId)` (Primary Constructor)
  * Initializes a new instance with the ID of the started process.

#### Properties
| Property | Type | Access | Description |
| :--- | :--- | :--- | :--- |
| `ProcessId` | `int` | `get` | The operating system process ID (PID) of the started command. |

#### Overridden Methods
* `override string ToString()`
  * **Attributes**: `[ExcludeFromCodeCoverage]`
  * **Returns**: Formatted string `$"Process ID: {ProcessId}"`

---

### 3. `StandardOutputCommandEvent`

* **Type**: `public class`
* **Base Class**: `CommandEvent`
* **Description**: Represents an event triggered when the running process writes a single line of text to its standard output (`stdout`) stream.

#### Constructors
* `StandardOutputCommandEvent(string text)` (Primary Constructor)
  * Initializes a new instance with the string emitted to stdout.

#### Properties
| Property | Type | Access | Description |
| :--- | :--- | :--- | :--- |
| `Text` | `string` | `get` | The line of text written to standard output. |

#### Overridden Methods
* `override string ToString()`
  * **Attributes**: `[ExcludeFromCodeCoverage]`
  * **Returns**: The value of the `Text` property directly.

---

### 4. `StandardErrorCommandEvent`

* **Type**: `public class`
* **Base Class**: `CommandEvent`
* **Description**: Represents an event triggered when the running process writes a single line of text to its standard error (`stderr`) stream.

#### Constructors
* `StandardErrorCommandEvent(string text)` (Primary Constructor)
  * Initializes a new instance with the string emitted to stderr.

#### Properties
| Property | Type | Access | Description |
| :--- | :--- | :--- | :--- |
| `Text` | `string` | `get` | The line of text written to standard error. |

#### Overridden Methods
* `override string ToString()`
  * **Attributes**: `[ExcludeFromCodeCoverage]`
  * **Returns**: The value of the `Text` property directly.

---

### 5. `ExitedCommandEvent`

* **Type**: `public class`
* **Base Class**: `CommandEvent`
* **Description**: Represents the event triggered when the process finishes execution. This event typically appears only once at the end of an event stream.

#### Constructors
* `ExitedCommandEvent(int exitCode)` (Primary Constructor)
  * Initializes a new instance with the final exit code of the process.

#### Properties
| Property | Type | Access | Description |
| :--- | :--- | :--- | :--- |
| `ExitCode` | `int` | `get` | The exit code returned by the terminated process (e.g., `0` for success). |

#### Overridden Methods
* `override string ToString()`
  * **Attributes**: `[ExcludeFromCodeCoverage]`
  * **Returns**: Formatted string `$"Exit code: {ExitCode}"`

---

## Technical Design Details

1. **C# Primary Constructors**: All concrete derived classes utilize C# primary constructor syntax (e.g., `public class StartedCommandEvent(int processId) : CommandEvent`).
2. **Immutable Property Design**: Properties on all derived events are read-only auto-properties (`{ get; }`) initialized by the primary constructor arguments, making the event instances immutable.
3. **Pattern Matching Focus**: `CommandEvent` is intentionally abstract and designed to be processed using pattern matching switch expressions or switch statements.
4. **Code Coverage Attribute**: The `ToString()` method overrides across all derived classes are decorated with `[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]`.

---

## Intended Usage Example

Using C# pattern matching with `CommandEvent`:

```csharp
CommandEvent cmdEvent = ...; // Obtained from an event stream

switch (cmdEvent)
{
    case StartedCommandEvent started:
        Console.WriteLine($"Process started with ID: {started.ProcessId}");
        break;

    case StandardOutputCommandEvent stdOut:
        Console.WriteLine($"Out: {stdOut.Text}");
        break;

    case StandardErrorCommandEvent stdErr:
        Console.WriteLine($"Err: {stdErr.Text}");
        break;

    case ExitedCommandEvent exited:
        Console.WriteLine($"Process exited with code: {exited.ExitCode}");
        break;
}
```