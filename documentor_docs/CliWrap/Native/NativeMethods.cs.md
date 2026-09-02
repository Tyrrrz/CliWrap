# Technical Documentation: `CliWrap/Native/NativeMethods.cs`

## Overview

The `NativeMethods.cs` file provides internal Platform Invoke (P/Invoke) bindings to interoperate with underlying operating system native libraries. Specifically, this file defines the interop signatures required to invoke the C standard library (`libc`) `kill` function on Unix-like operating systems.

---

## Code Structure & Namespace

* **Namespace**: `CliWrap.Native`
* **Accessibility**: `internal`
* **Class Hierarchy**:
  * `NativeMethods` (`internal static partial class`)
    * `Unix` (`public static partial class`)

---

## Key Components

### 1. `NativeMethods` Class
```csharp
internal static partial class NativeMethods
```
An internal static partial container class used to group native interop declarations for the library.

---

### 2. `NativeMethods.Unix` Class
```csharp
public static partial class Unix
```
A nested static partial class containing native interop declarations specific to Unix-based environments.

---

### 3. `Kill` Method
The `Kill` method imports the standard `kill` function from the native `libc` library.

#### Signature
```csharp
public static [partial | extern] int Kill(int pid, int sig);
```

#### Parameters
* **`pid`** (`int`): The target process identifier (PID).
* **`sig`** (`int`): The signal number to send to the specified process.

#### Return Value
* **`int`**: An integer status code returned directly by the native `libc` function call (typically `0` on success, or `-1` on failure).

---

## Technical Details & Implementation

### Conditional Compilation & Interop Attributes

The implementation uses conditional compilation directives (`#if NET7_0_OR_GREATER`) to optimize and maintain compatibility across different .NET target frameworks:

```csharp
#if NET7_0_OR_GREATER
        [LibraryImport("libc", EntryPoint = "kill", SetLastError = true)]
        public static partial int Kill(int pid, int sig);
#else
        [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
        public static extern int Kill(int pid, int sig);
#endif
```

#### 1. `.NET 7.0` or Greater (`NET7_0_OR_GREATER`)
* **Attribute**: `[LibraryImport]`
* **Mechanism**: Uses C# source generators at compile-time to generate P/Invoke marshalling code.
* **Requirements**: Method must be declared as `partial`.
* **Reasoning**: Improves runtime performance by eliminating runtime stub generation, leveraging APIs introduced in .NET 7.

#### 2. Target Frameworks Prior to .NET 7 (`#else`)
* **Attribute**: `[DllImport]`
* **Mechanism**: Uses runtime JIT P/Invoke marshalling.
* **Requirements**: Method must be declared as `extern`.

### Shared Attribute Properties
Both `LibraryImport` and `DllImport` configurations define the following settings:
* **Library Name**: `"libc"` (The standard C library on Unix-like operating systems).
* **`EntryPoint = "kill"`**: Explicitly specifies the native symbol name in `libc` to call.
* **`SetLastError = true`**: Instructs the runtime to capture the native error code (e.g., `errno`) set by the native function call so it can be retrieved via standard .NET APIs (e.g., `Marshal.GetLastPInvokeError()` or `Marshal.GetLastWin32Error()`).