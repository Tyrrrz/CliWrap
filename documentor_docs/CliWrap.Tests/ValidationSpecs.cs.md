# Technical Documentation: `ValidationSpecs.cs`

## Overview

The `ValidationSpecs.cs` file contains unit/integration tests for the `CliWrap` library under the `CliWrap.Tests` namespace. The primary purpose of this class is to test and verify how `CliWrap` handles command exit code validation, error handling, and exception throwing when executing processes that exit with non-zero exit codes.

---

## Class Information

* **Namespace:** `CliWrap.Tests`
* **Class Name:** `ValidationSpecs`
* **Constructor:** `ValidationSpecs(ITestOutputHelper testOutput)`
  * Utilizes C# primary constructor syntax to inject xUnit's `ITestOutputHelper` for logging test output during execution.

---

## Key Dependencies

* `CliWrap`: Core library components (`Cli`, `CommandResultValidation`).
* `CliWrap.Buffered`: Provides the `ExecuteBufferedAsync()` extension method.
* `CliWrap.Exceptions`: Provides `CommandExecutionException` thrown on validation failure.
* `FluentAssertions`: Provides fluid syntax for test assertions (`Should()`, `ThrowAsync()`, `Be()`, `BeEquivalentTo()`).
* `Xunit` / `Xunit.Abstractions`: Testing framework attributes (`[Fact]`) and output logging interface (`ITestOutputHelper`).

---

## Test Cases Detailed Breakdown

All tests in this class are asynchronous (`async Task`) and are configured with a 15-second timeout (`Timeout = 15000`).

### 1. `I_can_try_to_execute_a_command_and_get_an_error_if_it_returns_a_non_zero_exit_code`

* **Purpose:** Verifies that standard execution (`ExecuteAsync`) of a process resulting in a non-zero exit code throws a `CommandExecutionException` by default.
* **Execution Flow:**
  1. **Arrange:** Creates a command targeted at `Dummy.Program.FilePath` with arguments `["exit", "1"]`.
  2. **Act:** Defines an asynchronous action executing `cmd.ExecuteAsync()`.
  3. **Assert:**
     * Asserts that `CommandExecutionException` is thrown.
     * Validates that `ex.ExitCode` is `1`.
     * Validates that `ex.Command` is equivalent to the original `cmd` object.
     * Logs the string representation of the exception (`ex.ToString()`) to `testOutput`.

---

### 2. `I_can_try_to_execute_a_command_with_buffering_and_get_a_detailed_error_if_it_returns_a_non_zero_exit_code`

* **Purpose:** Verifies that buffered execution (`ExecuteBufferedAsync`) of a command returning a non-zero exit code throws a `CommandExecutionException` containing detailed standard error information in its message.
* **Execution Flow:**
  1. **Arrange:** Creates a command targeted at `Dummy.Program.FilePath` with arguments `["exit", "1"]`.
  2. **Act:** Defines an asynchronous action executing `cmd.ExecuteBufferedAsync()`.
  3. **Assert:**
     * Asserts that `CommandExecutionException` is thrown.
     * Validates that `ex.Message` contains `"Exit code set to 1"`.
     * Validates that `ex.ExitCode` is `1`.
     * Validates that `ex.Command` is equivalent to the original `cmd` object.
     * Logs the string representation of the exception (`ex.ToString()`) to `testOutput`.

---

### 3. `I_can_execute_a_command_without_validating_the_exit_code`

* **Purpose:** Verifies that disabling exit code validation using `.WithValidation(CommandResultValidation.None)` allows a command returning a non-zero exit code to execute without throwing an exception.
* **Execution Flow:**
  1. **Arrange:** Configures a command targeted at `Dummy.Program.FilePath` with arguments `["exit", "1"]` and explicit validation setting `CommandResultValidation.None`.
  2. **Act:** Executes the command asynchronously via `cmd.ExecuteAsync()`.
  3. **Assert:**
     * Confirms no exception is thrown during execution.
     * Asserts that `result.ExitCode` is `1`.
     * Asserts that `result.IsSuccess` is `false`.