# Technical Documentation: `CliWrap/CommandResultValidation.cs`

## Overview

The `CliWrap/CommandResultValidation.cs` file defines the `CommandResultValidation` enumeration within the `CliWrap` namespace. This enumeration serves as a bitfield strategy for specifying how the execution result of a command should be validated.

---

## Declaration Details

* **Namespace:** `CliWrap`
* **Type:** `enum`
* **Attributes:** `[Flags]`

The `[Flags]` attribute indicates that `CommandResultValidation` can be treated as a bitfield—that is, a set of flags that can be combined using bitwise operators.

---

## Enum Members

The `CommandResultValidation` enum contains the following members defined using binary literals:

| Member Name | Binary Value | Integer Value | Description |
| :--- | :--- | :--- | :--- |
| `None` | `0b0` | `0` | Performs no validation on the command execution result. |
| `ZeroExitCode` | `0b1` | `1` | Ensures that the executed command returned an exit code of zero (`0`). |

---

## Detailed Member Explanation

### `None` (`0b0`)
* **Value:** `0` (binary `0b0`)
* **Purpose:** Represents a state where no validation logic is applied to the result of a command execution. When this flag is used, any exit code returned by the command is considered acceptable.

### `ZeroExitCode` (`0b1`)
* **Value:** `1` (binary `0b1`)
* **Purpose:** Instructs the execution logic to validate that the command exited with a code of `0`. Non-zero exit codes fail this validation check.

---

## How It Works

1. **Bitwise Flag Design:**
   By decorating the enum with the `[Flags]` attribute and using binary representations (`0b0`, `0b1`), `CommandResultValidation` supports bitwise operations (`AND`, `OR`, `XOR`).
   
2. **Strategy Selection:**
   Callers select a validation mode by passing one or a combination of `CommandResultValidation` flags to configure command execution behavior. 
   * `CommandResultValidation.None` disables validation.
   * `CommandResultValidation.ZeroExitCode` enables zero-exit-code checks.