# Code Documentation: `CliWrap/Exceptions/CliWrapException.cs`

## Overview

The `CliWrapException` class serves as the abstract base class for all exceptions thrown by the `CliWrap` library. By inheriting from `System.Exception`, it establishes a common exception hierarchy specific to `CliWrap`, enabling consumers of the library to catch all library-specific exceptions under a single exception type.

---

## Class Declaration

```csharp
namespace CliWrap.Exceptions;

public abstract class CliWrapException(string message, Exception? innerException = null)
    : Exception(message, innerException)
```

- **Namespace:** `CliWrap.Exceptions`
- **Modifiers:** `public`, `abstract`
- **Inherits From:** `System.Exception`

Because the class is declared as `abstract`, it cannot be instantiated directly. It must be derived from by concrete exception classes within the library.

---

## Constructors

### 1. Primary Constructor

```csharp
public abstract class CliWrapException(string message, Exception? innerException = null)
    : Exception(message, innerException)
```

The class uses C# primary constructor syntax to initialize the base `System.Exception`.

#### Parameters
* **`message`** (`string`): The error message that describes the reason for the exception.
* **`innerException`** (`Exception?`, optional): The exception that is the cause of the current exception. Defaults to `null` if omitted.

---

### 2. Derived Constructor (Legacy Overload)

```csharp
[ExcludeFromCodeCoverage]
protected CliWrapException(string message)
    : this(message, null) { }
```

A `protected` secondary constructor that accepts only an error message and forwards the call to the primary constructor with a `null` inner exception.

#### Parameters
* **`message`** (`string`): The error message that describes the reason for the exception.

#### Attributes & Notes
* **`[ExcludeFromCodeCoverage]`**: Excludes this constructor from code coverage statistics.
* **Code Comment (`// TODO`)**: Indicates a planned future breaking change to remove this secondary constructor in favor of the primary constructor's optional parameter.

---

## Key Components Summary

| Component | Type | Description |
| :--- | :--- | :--- |
| `CliWrapException` | Class (`abstract`) | Base exception type for all `CliWrap` library exceptions. |
| `message` | Parameter (`string`) | Represents the error message describing the exception. |
| `innerException` | Parameter (`Exception?`) | Optional reference to an inner exception that triggered this exception. |
| `[ExcludeFromCodeCoverage]` | Attribute | Indicates that the secondary constructor is excluded from unit test code coverage analysis. |