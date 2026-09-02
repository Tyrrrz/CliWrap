# Technical Documentation: `CliWrap.Command`

The `CliWrap.Command` class represents an immutable set of instructions and configuration parameters required to run an external executable process. It serves as the primary configuration model within the CliWrap library.

---

## 1. Class Overview

* **Namespace:** `CliWrap`
* **Class Signature:** `public partial class Command : ICommandConfiguration`
* **Design Pattern:** Immutable Value Object / Fluent Builder Pattern

The `Command` class holds all parameters necessary for running a process—such as the target binary path, arguments, working directory, credentials, environment variables, resource policies, validation options, and standard I/O redirection pipes. 

Because `Command` is immutable, all state modifications via `With*` methods return a new instance of `Command` with updated configuration properties.

---

## 2. Primary Constructor and Default Configuration

The class leverages C# primary constructor syntax to accept all 10 configuration parameters upon instantiation.

### Primary Constructor Signature

```csharp
public Command(
    string targetFilePath,
    string arguments,
    string workingDirPath,
    ResourcePolicy resourcePolicy,
    Credentials credentials,
    IReadOnlyDictionary<string, string?> environmentVariables,
    CommandResultValidation validation,
    PipeSource standardInputPipe,
    PipeTarget standardOutputPipe,
    PipeTarget standardErrorPipe
)
```

### Default Constructor Overload

A convenience constructor accepts only a `targetFilePath` and sets all other parameters to default values:

```csharp
public Command(string targetFilePath)
```

#### Default Values Summary

| Parameter | Type | Default Value |
| :--- | :--- | :--- |
| `targetFilePath` | `string` | User provided |
| `arguments` | `string` | `string.Empty` |
| `workingDirPath` | `string` | `Directory.GetCurrentDirectory()` |
| `resourcePolicy` | `ResourcePolicy` | `ResourcePolicy.Default` |
| `credentials` | `Credentials` | `Credentials.Default` |
| `environmentVariables` | `IReadOnlyDictionary<string, string?>` | Empty `Dictionary<string, string?>` |
| `validation` | `CommandResultValidation` | `CommandResultValidation.ZeroExitCode` |
| `standardInputPipe` | `PipeSource` | `PipeSource.Null` |
| `standardOutputPipe` | `PipeTarget` | `PipeTarget.Null` |
| `standardErrorPipe` | `PipeTarget` | `PipeTarget.Null` |

---

## 3. Properties

All properties implement the `ICommandConfiguration` interface and are read-only (`{ get; }`).

* **`TargetFilePath` (`string`)**: Path to the target binary or executable file.
* **`Arguments` (`string`)**: Formatted command-line arguments string.
* **`WorkingDirPath` (`string`)**: Directory path where the process will execute.
* **`ResourcePolicy` (`ResourcePolicy`)**: Policy governing process resource allocation and limits.
* **`Credentials` (`Credentials`)**: User credentials (domain, username, password) used to run the process.
* **`EnvironmentVariables` (`IReadOnlyDictionary<string, string?>`)**: Set of environment variables provided to the child process.
* **`Validation` (`CommandResultValidation`)**: Defines process exit code validation strategy (e.g., ensuring a zero exit code).
* **`StandardInputPipe` (`PipeSource`)**: Source pipe directing input into the process standard input (`stdin`).
* **`StandardOutputPipe` (`PipeTarget`)**: Target pipe receiving output from the process standard output (`stdout`).
* **`StandardErrorPipe` (`PipeTarget`)**: Target pipe receiving output from the process standard error (`stderr`).

---

## 4. Fluent Transformation Methods ("With" Overloads)

Every transformation method creates and returns a new `Command` instance, preserving immutability. All transformation methods are decorated with the `[Pure]` attribute, signaling to static analysis tools that these invocations have no side effects on the existing instance.

### 4.1 Target File

* **`WithTargetFile(string targetFilePath)`**
  * **Returns:** A new `Command` instance with the specified executable path.

### 4.2 Arguments Configuration

* **`WithArguments(string arguments)`**
  * **Description:** Directly sets the raw arguments string.
  * **Warning:** Requires manual escaping. Incorrect formatting can cause runtime errors or security vulnerabilities (e.g., argument injection).
* **`WithArguments(IEnumerable<string> arguments, bool escape)`**
  * **Description:** Accepts a collection of arguments and configures them using `ArgumentsBuilder`.
* **`WithArguments(IEnumerable<string> arguments)`**
  * **Description:** Convenience overload that calls `WithArguments(arguments, true)` (enables escaping by default).
* **`WithArguments(Action<ArgumentsBuilder> configure)`**
  * **Description:** Configures arguments dynamically via a builder delegate callback.

### 4.3 Working Directory

* **`WithWorkingDirectory(string workingDirPath)`**
  * **Returns:** A new `Command` instance targeting the specified directory path.

### 4.4 Resource Policy

* **`WithResourcePolicy(ResourcePolicy resourcePolicy)`**
  * **Returns:** A new `Command` instance with the given `ResourcePolicy`.
* **`WithResourcePolicy(Action<ResourcePolicyBuilder> configure)`**
  * **Returns:** A new `Command` constructed via a `ResourcePolicyBuilder` delegate.

### 4.5 Credentials

* **`WithCredentials(Credentials credentials)`**
  * **Returns:** A new `Command` instance with specified user credentials.
* **`WithCredentials(Action<CredentialsBuilder> configure)`**
  * **Returns:** A new `Command` constructed using a `CredentialsBuilder` delegate.

### 4.6 Environment Variables

* **`WithEnvironmentVariables(IReadOnlyDictionary<string, string?> environmentVariables)`**
  * **Returns:** A new `Command` instance with the specified environment variables dictionary.
* **`WithEnvironmentVariables(Action<EnvironmentVariablesBuilder> configure)`**
  * **Returns:** A new `Command` instance constructed using an `EnvironmentVariablesBuilder` delegate.

### 4.7 Validation Policy

* **`WithValidation(CommandResultValidation validation)`**
  * **Returns:** A new `Command` instance configured with the specified result validation policy.

### 4.8 Standard Standard I/O Piping

* **`WithStandardInputPipe(PipeSource source)`**
  * **Returns:** A new `Command` instance with standard input redirected from `source`.
* **`WithStandardOutputPipe(PipeTarget target)`**
  * **Returns:** A new `Command` instance with standard output redirected to `target`.
* **`WithStandardErrorPipe(PipeTarget target)`**
  * **Returns:** A new `Command` instance with standard error redirected to `target`.

---

## 5. Overridden Methods

### `ToString()`

```csharp
[ExcludeFromCodeCoverage]
public override string ToString() => $"{TargetFilePath} {Arguments}";
```

* **Description:** Returns a string combining `TargetFilePath` and `Arguments`, separated by a single space.
* **Attributes:** Marked with `[ExcludeFromCodeCoverage]`.