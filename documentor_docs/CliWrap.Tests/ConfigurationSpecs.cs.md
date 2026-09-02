# Documentation Guide: `CliWrap.Tests/ConfigurationSpecs.cs`

## Overview

The `ConfigurationSpecs.cs` file contains a unit test suite for verifying the command configuration functionality within the `CliWrap` framework. Written using **xUnit** as the test runner and **FluentAssertions** for assertions, these specs validate that command objects:
1. Initialize with correct default property values upon creation via `Cli.Wrap(...)`.
2. Can be reconfigured using various fluent extension/builder methods.
3. Maintain **immutability** when reconfigured (i.e., calling configuration modification methods returns a new instance with updated properties while leaving the original command object unchanged).

---

## Dependencies & Frameworks

- **`CliWrap`**: The library under test (`Cli`, `ResourcePolicy`, `Credentials`, `CommandResultValidation`, `PipeSource`, `PipeTarget`).
- **`Xunit`**: Test framework providing the `[Fact]` attributes.
- **`FluentAssertions`**: Assertion library providing readable specification checks (`Should().Be(...)`, `Should().BeEquivalentTo(...)`, `Excluding(...)`).
- **`System.Diagnostics`**: Provides `ProcessPriorityClass` used in resource policy tests.
- **`System.IO`**: Provides `Directory` and `Stream` references.
- **`System.Collections.Generic`**: Provides `Dictionary<K, V>` support for environment variable tests.

---

## Testing Strategy & Pattern

All mutation tests follow the **Arrange-Act-Assert** pattern and verify object immutability:

1. **Arrange**: Create an `original` command object using `Cli.Wrap("foo")` and any initial settings.
2. **Act**: Invoke a `With*` method (e.g., `WithTargetFile`, `WithArguments`) to produce a `modified` command instance.
3. **Assert**: 
   - Verify that `original` matches `modified` for all properties *except* the one targeted for change (`Excluding(...)`).
   - Verify that the target property on `original` differs from `modified`.
   - Verify that `modified` contains the exact expected new configuration value.

---

## Summary of Tested Properties and Defaults

The test `I_can_create_a_command_with_the_default_configuration` establishes the default state of a newly created `Command` object via `Cli.Wrap("foo")`:

| Property | Default Value | Mutation Method Tested |
| :--- | :--- | :--- |
| `TargetFilePath` | `"foo"` | `WithTargetFile(...)` |
| `Arguments` | `""` (Empty) | `WithArguments(...)` |
| `WorkingDirPath` | `Directory.GetCurrentDirectory()` | `WithWorkingDirectory(...)` |
| `ResourcePolicy` | `ResourcePolicy.Default` | `WithResourcePolicy(...)` |
| `Credentials` | `Credentials.Default` | `WithCredentials(...)` |
| `EnvironmentVariables` | `Empty` | `WithEnvironmentVariables(...)` |
| `Validation` | `CommandResultValidation.ZeroExitCode` | `WithValidation(...)` |
| `StandardInputPipe` | `PipeSource.Null` | `WithStandardInputPipe(...)` |
| `StandardOutputPipe` | `PipeTarget.Null` | `WithStandardOutputPipe(...)` |
| `StandardErrorPipe` | `PipeTarget.Null` | `WithStandardErrorPipe(...)` |

---

## Detailed Test Method Breakdown

### 1. Default Configuration
* **`I_can_create_a_command_with_the_default_configuration`**
  - **Purpose**: Asserts that `Cli.Wrap("foo")` sets initial default values across all properties.
  - **Assertions**: Validates default target file path, empty arguments/environment variables, current working directory, default resource policy, default credentials, `ZeroExitCode` validation strategy, and null pipes for stdin, stdout, and stderr.

### 2. Target File Path Configuration
* **`I_can_configure_the_target_file`**
  - **Purpose**: Verifies that `WithTargetFile("bar")` updates the `TargetFilePath` property while preserving immutability.

### 3. Command Line Arguments Configuration
* **`I_can_configure_the_command_line_arguments`**
  - **Method**: `WithArguments(string)`
  - **Purpose**: Confirms direct string replacement of argument string.
* **`I_can_configure_the_command_line_arguments_by_passing_an_array`**
  - **Method**: `WithArguments(string[])`
  - **Purpose**: Verifies array passing with automatic string formatting and escaping (`["-a", "foo bar"]` becomes `-a "foo bar"`).
* **`I_can_configure_the_command_line_arguments_using_a_builder`**
  - **Method**: `WithArguments(Action<ArgumentsBuilder>)`
  - **Purpose**: Verifies builder pattern support for adding single arguments, pre-quoted/escaped strings, numeric values (`double`), string arrays, and numeric arrays. Checks that the formatted result equals `-a "foo bar" "\"foo\\bar\"" 3.14 foo bar -5 89.13`.

### 4. Working Directory Configuration
* **`I_can_configure_the_working_directory`**
  - **Method**: `WithWorkingDirectory(string)`
  - **Purpose**: Verifies updating the process's working directory path.

### 5. Resource Policy Configuration
* **`I_can_configure_the_resource_policy`**
  - **Method**: `WithResourcePolicy(ResourcePolicy)`
  - **Purpose**: Verifies setting a `ResourcePolicy` instance directly (`ProcessPriorityClass.High`, affinity `0x1`, min working set `1024`, max working set `2048`).
* **`I_can_configure_the_resource_policy_using_a_builder`**
  - **Method**: `WithResourcePolicy(Action<ResourcePolicyBuilder>)`
  - **Purpose**: Verifies configuring resource policy using builder calls (`SetPriority`, `SetAffinity`, `SetMinWorkingSet`, `SetMaxWorkingSet`).

### 6. User Credentials Configuration
* **`I_can_configure_the_user_credentials`**
  - **Method**: `WithCredentials(Credentials)`
  - **Purpose**: Verifies setting explicit `Credentials` object (`domain`, `username`, `password`, `loadUserProfile: true`).
* **`I_can_configure_the_user_credentials_using_a_builder`**
  - **Method**: `WithCredentials(Action<CredentialsBuilder>)`
  - **Purpose**: Verifies fluent configuration via builder methods (`SetDomain`, `SetUserName`, `SetPassword`, `LoadUserProfile`).

### 7. Environment Variables Configuration
* **`I_can_configure_the_environment_variables`**
  - **Method**: `WithEnvironmentVariables(IReadOnlyDictionary<string, string?>)`
  - **Purpose**: Verifies setting environment variables using a dictionary payload.
* **`I_can_configure_the_environment_variables_using_a_builder`**
  - **Method**: `WithEnvironmentVariables(Action<EnvironmentVariablesBuilder>)`
  - **Purpose**: Verifies builder pattern methods (`Set(key, value)` and `Set(dictionary)`) for setting multiple environment variables.

### 8. Result Validation Strategy
* **`I_can_configure_the_result_validation_strategy`**
  - **Method**: `WithValidation(CommandResultValidation)`
  - **Purpose**: Verifies modifying the validation behavior (e.g., setting from `CommandResultValidation.ZeroExitCode` to `CommandResultValidation.None`).

### 9. I/O Pipe Configuration
* **`I_can_configure_the_stdin_pipe`**
  - **Method**: `WithStandardInputPipe(PipeSource)`
  - **Purpose**: Verifies configuring `StandardInputPipe` with `PipeSource.FromString("new")`.
* **`I_can_configure_the_stdout_pipe`**
  - **Method**: `WithStandardOutputPipe(PipeTarget)`
  - **Purpose**: Verifies configuring `StandardOutputPipe` with `PipeTarget.ToStream(Stream.Null)`.
* **`I_can_configure_the_stderr_pipe`**
  - **Method**: `WithStandardErrorPipe(PipeTarget)`
  - **Purpose**: Verifies configuring `StandardErrorPipe` with `PipeTarget.ToStream(Stream.Null)`.