# Technical Documentation: `CliWrap.Tests/ResourcePolicySpecs.cs`

## Overview

The `ResourcePolicySpecs` class contains integration tests for validating the resource policy capabilities of the **CliWrap** library. It ensures that commands configured with custom process execution policies—such as process priority, CPU core affinity, and working set limits—execute correctly on supported operating systems and fail gracefully with appropriate exceptions on unsupported platforms.

---

## Class Information

- **Namespace**: `CliWrap.Tests`
- **Class Name**: `ResourcePolicySpecs`
- **Primary Dependencies**:
  - `System`
  - `System.Diagnostics`
  - `System.Threading.Tasks`
  - `FluentAssertions`
  - `Xunit`

---

## Test Characteristics & Execution Policy

All test methods within this class share common execution attributes:
* **`[SkippableFact(Timeout = 15000)]`**: Each test is configured with a 15-second timeout and utilizes `Xunit.SkippableFact` to dynamically skip execution depending on the host Operating System (OS).

---

## Detailed Test Case Specifications

### 1. `I_can_execute_a_command_with_a_custom_process_priority`

* **Purpose**: Tests executing a command with a custom process priority class.
* **Platform Constraints**: Executed **only on Windows** (`OperatingSystem.IsWindows()`). On non-Windows platforms, setting priority requires elevated permissions that cannot be guaranteed in CI environments, so the test is skipped.
* **Configuration**:
  * Configures resource policy using `.WithResourcePolicy(p => p.SetPriority(ProcessPriorityClass.High))`.
  * Command executable: `Dummy.Program.FilePath`.
* **Behavior**:
  1. Checks if OS is Windows; skips if false.
  2. Builds command with `ProcessPriorityClass.High`.
  3. Executes asynchronously via `ExecuteAsync()`.
  4. Asserts that the process exit code is `0`.

---

### 2. `I_can_execute_a_command_with_a_custom_core_affinity`

* **Purpose**: Tests executing a command restricted to run on specific CPU cores (core affinity mask).
* **Platform Constraints**: Supported and executed on **Windows and Linux** (`OperatingSystem.IsWindows() || OperatingSystem.IsLinux()`). Skips on other operating systems.
* **Configuration**:
  * Configures core affinity using `.WithResourcePolicy(p => p.SetAffinity(0b1010))`.
  * The bitmask `0b1010` targets CPU Core 1 and Core 3.
* **Behavior**:
  1. Verifies OS is Windows or Linux; skips otherwise.
  2. Builds command with the specified bitmask affinity.
  3. Executes asynchronously via `ExecuteAsync()`.
  4. Asserts that the process exit code is `0`.

---

### 3. `I_can_execute_a_command_with_a_custom_working_set_limit`

* **Purpose**: Tests executing a command with custom minimum and maximum working set size (memory) limits.
* **Platform Constraints**: Executed **only on Windows** (`OperatingSystem.IsWindows()`).
* **Configuration**:
  * Minimum working set: `1024 * 1024` bytes (1 MB).
  * Maximum working set: `1024 * 1024 * 10` bytes (10 MB).
  * Configured via `.WithResourcePolicy(p => p.SetMinWorkingSet(...).SetMaxWorkingSet(...))`.
* **Behavior**:
  1. Checks if OS is Windows; skips if false.
  2. Builds command with memory working set bounds.
  3. Executes asynchronously via `ExecuteAsync()`.
  4. Asserts that the process exit code is `0`.

---

### 4. `I_can_try_to_execute_a_command_with_a_custom_resource_policy_and_get_an_error_if_the_operating_system_does_not_support_it`

* **Purpose**: Verifies that attempting to set an unsupported resource policy (such as working set limits) on an unsupported OS throws a `NotSupportedException`.
* **Platform Constraints**: Executed on **non-Windows platforms** (`Skip.If(OperatingSystem.IsWindows())`).
* **Configuration**:
  * Configures resource policy using `.WithResourcePolicy(p => p.SetMinWorkingSet(1024 * 1024))`.
* **Behavior**:
  1. Skips test if running on Windows.
  2. Attempts to execute the command asynchronously with working set limits configured.
  3. Asserts that `cmd.ExecuteAsync()` throws `System.NotSupportedException`.

---

## Methods & Configurations Tested

| Method Called | Parameter / Value | Purpose |
| :--- | :--- | :--- |
| `p.SetPriority(...)` | `ProcessPriorityClass.High` | Sets CPU scheduling priority for the process. |
| `p.SetAffinity(...)` | `IntPtr` / `0b1010` bitmask | Binds execution to specific CPU cores. |
| `p.SetMinWorkingSet(...)` | `1024 * 1024` (1 MB) | Configures minimum process memory allocation. |
| `p.SetMaxWorkingSet(...)` | `1024 * 1024 * 10` (10 MB) | Configures maximum process memory allocation. |