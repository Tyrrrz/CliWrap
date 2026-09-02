# Technical Documentation: `CliWrap.Tests/EnvironmentSpecs.cs`

## Overview

The `EnvironmentSpecs.cs` file contains unit/integration tests for the **CliWrap** library, specifically focusing on validating how CLI commands handle environmental configurations such as **working directories** and **environment variables** (adding, keeping, overwriting, and unsetting variables).

- **Namespace**: `CliWrap.Tests`
- **Class**: `EnvironmentSpecs`

---

## Dependencies

The file relies on the following namespaces and external packages:

- `System`, `System.Collections.Generic`, `System.IO`, `System.Threading.Tasks`: Core C# system libraries for file IO, dictionary usage, async tasks, and GUID generation.
- `CliWrap.Buffered`: Provides buffered execution extensions (`ExecuteBufferedAsync`).
- `CliWrap.Tests.Utils.Extensions`: Custom testing utilities and assertions (e.g., `ConsistOfLines`).
- `FluentAssertions`: Fluent assertion library used for test assertions (`Should()`).
- `PowerKit`, `PowerKit.Extensions`: Utility extensions (e.g., `TempDirectory`, `TrimPrefix`, `SetTempEnvironmentVariable`).
- `Xunit`: Testing framework providing `[Fact]` attributes.

---

## Class Structure & Test Methods

The `EnvironmentSpecs` class contains three asynchronous unit tests, each decorated with `[Fact(Timeout = 15000)]` (ensuring each test fails if it exceeds a 15-second timeout execution window).

```
EnvironmentSpecs
 ├── I_can_execute_a_command_with_a_custom_working_directory()
 ├── I_can_execute_a_command_with_additional_environment_variables()
 └── I_can_execute_a_command_with_some_environment_variables_overwritten()
```

---

### Test Methods Detail

#### 1. `I_can_execute_a_command_with_a_custom_working_directory()`

- **Purpose**: Verifies that a command executed via `CliWrap` respects a custom working directory defined via `.WithWorkingDirectory(...)`.
- **Execution Flow**:
  1. **Arrange**:
     - Creates a temporary directory using `TempDirectory.Create()`.
     - Configures a command using `Cli.Wrap(Dummy.Program.FilePath)` with argument `"cwd"` and sets its working directory to `dir.Path`.
  2. **Act**:
     - Executes the command asynchronously with output buffering via `cmd.ExecuteBufferedAsync()`.
  3. **Assert**:
     - Cleans up standard output by removing trailing whitespace and stripping macOS specific `/private` symlink prefixes.
     - Resolves the absolute path using `Path.GetFullPath(...)`.
     - Asserts that the normalized output path equals the absolute path of the created temporary directory (`Path.GetFullPath(dir.Path)`).

---

#### 2. `I_can_execute_a_command_with_additional_environment_variables()`

- **Purpose**: Verifies that custom environment variables can be added to the execution context using a dictionary via `.WithEnvironmentVariables(...)`.
- **Execution Flow**:
  1. **Arrange**:
     - Defines a dictionary `env` with two environment variable pairs: `{"foo": "bar"}` and `{"hello": "world"}`.
     - Builds a CLI command with arguments `["env", "foo", "hello"]` and attaches the dictionary using `.WithEnvironmentVariables(env)`.
  2. **Act**:
     - Executes the command using `cmd.ExecuteBufferedAsync()`.
  3. **Assert**:
     - Asserts that the standard output consists of lines matching `"bar"` and `"world"` in order.

---

#### 3. `I_can_execute_a_command_with_some_environment_variables_overwritten()`

- **Purpose**: Validates the fluid configuration delegate for environment variables (`WithEnvironmentVariables(Action<EnvironmentVariableBuilder>)`), testing variable preservation, overwriting, and unsetting.
- **Execution Flow**:
  1. **Arrange**:
     - Generates a unique key using `Guid.NewGuid()`.
     - Formats three distinct variable names: `variableToKeep`, `variableToOverwrite`, and `variableToUnset`.
     - Sets process-level temporary environment variables inside a `using` block:
       - `variableToKeep` set to `"keep"` (will remain untouched).
       - `variableToOverwrite` set to `"overwrite"` (will be modified).
       - `variableToUnset` set to `"unset"` (will be removed/nullified).
     - Configures the command with arguments `["env", variableToKeep, variableToOverwrite, variableToUnset]`.
     - Applies fluid environment variable mutations via `WithEnvironmentVariables`:
       - Sets `variableToOverwrite` to `"overwritten"`.
       - Sets `variableToUnset` to `null`.
  2. **Act**:
     - Executes the command using `cmd.ExecuteBufferedAsync()`.
  3. **Assert**:
     - Asserts that standard output contains lines `"keep"` and `"overwritten"`. The unset variable outputs nothing (is omitted from output).

---

## Key Test Utilities & Conventions Used

- **`Dummy.Program.FilePath`**: Refers to a dummy executable helper path used across integration tests to simulate a standard process responding to specific argument commands (`"cwd"`, `"env"`).
- **`TempDirectory.Create()`**: Creates a temporary file directory managed within a `using` declaration for auto-cleanup.
- **`SetTempEnvironmentVariable`**: Temporarily sets environment variables scoped to a `using` block.
- **`TrimPrefix("/private")`**: Handles platform normalization specifically for macOS where `/tmp` maps to `/private/tmp`.