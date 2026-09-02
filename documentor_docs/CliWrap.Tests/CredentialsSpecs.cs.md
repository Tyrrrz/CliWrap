# Technical Documentation: `CliWrap.Tests/CredentialsSpecs.cs`

## Overview

The `CredentialsSpecs.cs` file contains integration test specifications for validating CliWrap's process credentials functionality (`WithCredentials`). It ensures that execution of command-line processes under custom user credentials (username, password, domain, profile loading) behaves predictably across supported and unsupported operating systems.

The test suite evaluates two primary runtime scenarios:
1. **Windows Platform**: Verifies that custom user credentials (with or without domain specification and profile loading) are correctly passed down to the underlying OS execution engine (verified via a `Win32Exception` when using dummy credentials).
2. **Non-Windows Platforms**: Verifies that attempting to run a process with custom user credentials on an unsupported OS throws a `NotSupportedException`.

---

## Class Information

- **Namespace**: `CliWrap.Tests`
- **Class**: `CredentialsSpecs`
- **Test Framework**: `xUnit` (with `Xunit.SkippableFact`)
- **Assertion Library**: `FluentAssertions`

---

## Test Cases Detailed Analysis

All test methods in this class use the `[SkippableFact(Timeout = 15000)]` attribute, giving each test a 15-second execution timeout and enabling dynamic test skipping based on runtime platform conditions.

---

### 1. `I_can_execute_a_command_as_a_different_user()`

#### Purpose
Verifies that on Windows, a command configured with a custom username, password, and profile loading instruction attempts execution under those credentials.

#### Implementation Details
- **Platform Constraint**: `Skip.IfNot(OperatingSystem.IsWindows(), ...)`
  - Skipped on non-Windows platforms.
- **Arrange**:
  - Targets `Dummy.Program.FilePath`.
  - Calls `.WithCredentials(...)` configuring:
    - Username: `"user123"` via `.SetUserName(...)`
    - Password: `"pass123"` via `.SetPassword(...)`
    - Profile option: `.LoadUserProfile()`
- **Act**:
  - Invokes `cmd.ExecuteAsync()`.
- **Assert**:
  - Expects `Win32Exception` to be thrown. Because `"user123"`/`"pass123"` are invalid system credentials, receiving a `Win32Exception` confirms that the OS attempted to authenticate and run the process under the provided credentials.

---

### 2. `I_can_execute_a_command_as_a_different_user_under_the_specified_domain()`

#### Purpose
Verifies that on Windows, a command configured with a custom domain, username, password, and profile loading instruction attempts execution under those domain credentials.

#### Implementation Details
- **Platform Constraint**: `Skip.IfNot(OperatingSystem.IsWindows(), ...)`
  - Skipped on non-Windows platforms.
- **Arrange**:
  - Targets `Dummy.Program.FilePath`.
  - Calls `.WithCredentials(...)` configuring:
    - Domain: `"domain123"` via `.SetDomain(...)`
    - Username: `"user123"` via `.SetUserName(...)`
    - Password: `"pass123"` via `.SetPassword(...)`
    - Profile option: `.LoadUserProfile()`
- **Act**:
  - Invokes `cmd.ExecuteAsync()`.
- **Assert**:
  - Expects `Win32Exception` to be thrown upon execution, validating that the domain credentials were processed by the underlying Windows execution layer.

---

### 3. `I_can_try_to_execute_a_command_as_a_different_user_and_get_an_error_if_the_operating_system_does_not_support_it()`

#### Purpose
Verifies that on non-Windows operating systems, attempting to execute a command with user credentials correctly throws a `NotSupportedException`.

#### Implementation Details
- **Platform Constraint**: `Skip.If(OperatingSystem.IsWindows(), ...)`
  - Skipped on Windows platforms.
- **Arrange**:
  - Targets `Dummy.Program.FilePath`.
  - Calls `.WithCredentials(...)` configuring:
    - Username: `"user123"` via `.SetUserName(...)`
    - Password: `"pass123"` via `.SetPassword(...)`
- **Act**:
  - Invokes `cmd.ExecuteAsync()`.
- **Assert**:
  - Asserts that execution throws a `NotSupportedException`.

---

## Tested API Surface

The code in this test file directly invokes and tests the following CliWrap credentials builder methods:

| Method | Description |
| :--- | :--- |
| `Cli.Wrap(...)` | Factory method to create a command builder using `Dummy.Program.FilePath`. |
| `.WithCredentials(...)` | Configures credential options for process execution. |
| `c.SetUserName(...)` | Sets the target user account name. |
| `c.SetPassword(...)` | Sets the password for the target user account. |
| `c.SetDomain(...)` | Sets the domain for the target user account. |
| `c.LoadUserProfile()` | Flag indicating whether to load the user profile upon starting the process. |
| `cmd.ExecuteAsync()` | Asynchronously starts and executes the configured command. |

---

## Expected Exceptions Summary

| Test Method | OS Environment | Expected Exception | Reason |
| :--- | :--- | :--- | :--- |
| `I_can_execute_a_command_as_a_different_user` | Windows | `Win32Exception` | Non-existent user credentials fail at the OS level. |
| `I_can_execute_a_command_as_a_different_user_under_the_specified_domain` | Windows | `Win32Exception` | Non-existent domain credentials fail at the OS level. |
| `I_can_try_to_execute_a_command...` | Non-Windows | `NotSupportedException` | Credentials execution mode is unsupported on non-Windows platforms. |