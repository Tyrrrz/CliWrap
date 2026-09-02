# Technical Documentation: `OutputTarget.cs`

**File Path:** `CliWrap.Tests.Dummy/Commands/Shared/OutputTarget.cs`

---

## Purpose

The `OutputTarget` enumeration defines bitwise flags that identify targeted standard output streams. It is located in the shared commands namespace for the dummy test executable (`CliWrap.Tests.Dummy`) and allows code to specify whether an action should target standard output (`StdOut`), standard error (`StdErr`), or both (`All`).

---

## Declaration Details

* **Namespace:** `CliWrap.Tests.Dummy.Commands.Shared`
* **Type:** `public enum OutputTarget`
* **Attributes:** `[Flags]` (from the `System` namespace)

---

## Key Components

### `[Flags]` Attribute
The `[Flags]` attribute indicates that the `OutputTarget` enumeration can be treated as a bitfield (a set of flags). This allows enumeration values to be combined using bitwise OR operations.

### Enumeration Members

| Name | Underlying Value | Bitwise Expression | Description |
| :--- | :--- | :--- | :--- |
| `StdOut` | `1` | `0001` (Binary) | Represents the Standard Output stream (`stdout`). |
| `StdErr` | `2` | `0010` (Binary) | Represents the Standard Error stream (`stderr`). |
| `All` | `3` | `StdOut \| StdErr` | Represents both `StdOut` and `StdErr` streams combined via bitwise OR. |

---

## How It Works

1. **Flag Assignment:** Individual stream flags are assigned powers of two (`StdOut = 1`, `StdErr = 2`), ensuring each flag corresponds to a unique bit position.
2. **Combination Value:** The `All` member is defined dynamically as `StdOut | StdErr`, which evaluates to `3` (binary `11`). This allows a caller to target both output streams simultaneously using either the combined flag `OutputTarget.All` or by performing a bitwise OR operation on `OutputTarget.StdOut | OutputTarget.StdErr`.