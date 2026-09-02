# Technical Documentation: `CliWrap.Tests/LineBreakSpecs.cs`

## Overview

The `LineBreakSpecs.cs` file contains a suite of asynchronous unit/integration tests within the `CliWrap.Tests` namespace. The primary purpose of this specification class is to verify that `CliWrap` correctly handles and splits standard output (`stdout`) stream data across various line ending formats (`\n`, `\r`, `\r\n`), including scenarios with consecutive line breaks that yield empty lines.

---

## Dependencies & Imports

The class relies on the following namespaces and test frameworks:

- `System.Collections.Generic`: Provides `List<T>` to collect output lines.
- `System.Threading.Tasks`: Supports asynchronous execution (`Task`).
- `FluentAssertions`: Used for expressive assertions (`Should().Equal(...)`).
- `Xunit`: Provides the testing framework attributes (`[Fact]`).

---

## Class Structure

### `LineBreakSpecs`

- **Namespace**: `CliWrap.Tests`
- **Scope**: `public class LineBreakSpecs`

This class contains four test methods. Each test executes a command using a target executable (`Dummy.Program.FilePath`) with the argument `"echo stdin"`. Standard input (`stdin`) is piped into the process, and standard output (`stdout`) is redirected to a string collection delegate (`stdOutLines.Add`).

---

## Common Execution Pattern

All test methods in this class follow a common pattern:

1. **Test Configuration**: Decorated with `[Fact(Timeout = 15000)]`, ensuring tests time out if execution exceeds 15,000 milliseconds (15 seconds).
2. **Arrange**:
   - Define a input string `data` containing text separated by specific line-break characters.
   - Initialize `var stdOutLines = new List<string>()` to store captured lines.
   - Pipe `data` as standard input into the dummy command configured via `Cli.Wrap(Dummy.Program.FilePath).WithArguments("echo stdin")`.
   - Pipe stdout lines into `stdOutLines.Add`.
3. **Act**:
   - Execute the command asynchronously using `await cmd.ExecuteAsync()`.
4. **Assert**:
   - Use `stdOutLines.Should().Equal(...)` from `FluentAssertions` to verify that stdout was split into the exact expected sequence of strings.

---

## Test Methods

### 1. `I_can_execute_a_command_and_split_the_stdout_by_newline()`

- **Purpose**: Verifies handling of Unix-style line feed (`\n`) characters.
- **Input (`data`)**: `"Foo\nBar\nBaz"`
- **Expected Result**: `["Foo", "Bar", "Baz"]`
- **Behavior**: Splits stdout into distinct lines whenever a `\n` character is encountered.

---

### 2. `I_can_execute_a_command_and_split_the_stdout_by_caret_return()`

- **Purpose**: Verifies handling of carriage return (`\r`) characters.
- **Input (`data`)**: `"Foo\rBar\rBaz"`
- **Expected Result**: `["Foo", "Bar", "Baz"]`
- **Behavior**: Splits stdout into distinct lines whenever a single `\r` character is encountered.

---

### 3. `I_can_execute_a_command_and_split_the_stdout_by_caret_return_followed_by_newline()`

- **Purpose**: Verifies handling of Windows-style line breaks (`\r\n`).
- **Input (`data`)**: `"Foo\r\nBar\r\nBaz"`
- **Expected Result**: `["Foo", "Bar", "Baz"]`
- **Behavior**: Treats `\r\n` as a single line separator and splits stdout accordingly without generating empty intermediate lines.

---

### 4. `I_can_execute_a_command_and_split_the_stdout_by_newline_while_including_empty_lines()`

- **Purpose**: Verifies handling of consecutive line-break characters (`\r\r`, `\n\n`) to ensure empty lines are preserved in output.
- **Input (`data`)**: `"Foo\r\rBar\n\nBaz"`
- **Expected Result**: `["Foo", "", "Bar", "", "Baz"]`
- **Behavior**: Treats consecutive line break tokens as separate line boundaries, emitting empty strings `""` for blank lines.

---

## Summary Matrix

| Test Method | Line Separator Type | Input String | Expected Elements |
| :--- | :--- | :--- | :--- |
| `I_can_execute_a_command_and_split_the_stdout_by_newline` | `\n` (Line Feed) | `"Foo\nBar\nBaz"` | `"Foo"`, `"Bar"`, `"Baz"` |
| `I_can_execute_a_command_and_split_the_stdout_by_caret_return` | `\r` (Carriage Return) | `"Foo\rBar\rBaz"` | `"Foo"`, `"Bar"`, `"Baz"` |
| `I_can_execute_a_command_and_split_the_stdout_by_caret_return_followed_by_newline` | `\r\n` (CRLF) | `"Foo\r\nBar\r\nBaz"` | `"Foo"`, `"Bar"`, `"Baz"` |
| `I_can_execute_a_command_and_split_the_stdout_by_newline_while_including_empty_lines` | Consecutive `\r\r` and `\n\n` | `"Foo\r\rBar\n\nBaz"` | `"Foo"`, `""`, `"Bar"`, `""`, `"Baz"` |