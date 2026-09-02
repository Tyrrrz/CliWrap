# Documentation: `CliWrap.Tests/PathResolutionSpecs.cs`

## Overview

The `PathResolutionSpecs` class is part of the `CliWrap.Tests` test suite. Its purpose is to verify that `CliWrap` can resolve and execute targets (both standard executables and platform-specific script files) using short names resolved via the system's `PATH` environment variable.

---

## Technical Dependencies

* **`System` / `System.IO` / `System.Threading.Tasks`**: Core .NET libraries for standard functionality, file system operations, and asynchronous operations.
* **`CliWrap.Buffered`**: Provides buffered execution extension methods (`ExecuteBufferedAsync`).
* **`CliWrap.Tests.Utils.Extensions`**: Custom test utilities/extensions (specifically `Environment.ExtendPath`).
* **`FluentAssertions`**: Fluent assertion framework for verifying test conditions (`Should().BeTrue()`, `Should().MatchRegex()`, `Should().Be()`).
* **`PowerKit`**: Utility library supplying temporary directory creation (`TempDirectory.Create()`).
* **`Xunit`**: Test framework providing test metadata (`[Fact]`, `[SkippableFact]`, `Skip.IfNot`).

---

## Test Suite Structure

**Namespace:** `CliWrap.Tests`  
**Class:** `PathResolutionSpecs`

The class contains two asynchronous test methods targeting executable and script path resolution.

---

## Test Specifications

### 1. `I_can_execute_a_command_on_an_executable_using_its_short_name()`

#### Purpose
Verifies that `CliWrap` can locate and execute a system binary (specifically `dotnet`) available on the default `PATH` environment variable using only its short name.

#### Attributes
* `[Fact(Timeout = 15000)]`: Standard xUnit test fact with a maximum execution timeout of 15,000 ms (15 seconds).

#### Workflow
1. **Arrange**: Construct a CLI command targeted at `"dotnet"` with the argument `"--version"`.
2. **Act**: Execute the command asynchronously using `ExecuteBufferedAsync()`.
3. **Assert**:
   * Verify `result.IsSuccess` is `true`.
   * Verify `result.StandardOutput` (trimmed) matches a semantic version regex (`^\d+\.\d+\.\d+$`).

---

### 2. `I_can_execute_a_command_on_a_script_using_its_short_name()`

#### Purpose
Verifies that on Windows environments, `CliWrap` can locate and execute a batch script (e.g., `.cmd`) located on a custom `PATH` directory using only the script's short name (without the file extension).

#### Attributes
* `[SkippableFact(Timeout = 15000)]`: xUnit test fact that can be conditionally skipped, with a maximum execution timeout of 15,000 ms (15 seconds).

#### Workflow
1. **Platform Check**: Calls `Skip.IfNot(OperatingSystem.IsWindows(), ...)` to skip execution on non-Windows operating systems, as script extension resolution via PATH is specific to Windows.
2. **Arrange**:
   * Creates a temporary directory via `TempDirectory.Create()`.
   * Writes a Windows batch script named `test-script.cmd` containing `@echo hello` to the temporary directory.
   * Temporarily appends the temporary directory path to the process `PATH` environment variable using `Environment.ExtendPath(dir.Path)`.
   * Constructs a command targeted at `"test-script"` (omitting `.cmd`).
3. **Act**: Executes the command asynchronously using `ExecuteBufferedAsync()`.
4. **Assert**:
   * Verify `result.IsSuccess` is `true`.
   * Verify `result.StandardOutput` (trimmed) equals `"hello"`.

---

## Key Utility Method Usage

* **`Cli.Wrap(...)`**: Standard entry point for configuring a command execution model.
* **`ExecuteBufferedAsync()`**: Executes the process asynchronously and buffers stdout and stderr streams into memory.
* **`TempDirectory.Create()`**: Creates a disposable directory structure cleaned up via `using` statements.
* **`Environment.ExtendPath(...)`**: Temporarily extends the current process `PATH` variable within a `using` block scope.