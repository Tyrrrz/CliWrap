# Technical Documentation: `ICommandConfiguration`

## Overview

The `ICommandConfiguration` interface in the `CliWrap` namespace provides a read-only contract for retrieving the configuration settings and instructions required to execute an external process. It exposes properties that define execution targets, arguments, working directories, environment variables, credentials, resource policies, result validation strategies, and stream piping configurations.

---

## Interface Definition

```csharp
namespace CliWrap;

public interface ICommandConfiguration
```

* **Namespace:** `CliWrap`
* **Access Modifier:** `public`
* **Type:** `interface`

---

## Properties

All properties defined in `ICommandConfiguration` are getter-only (`get;`), ensuring that the configuration state is read-only when accessed through this interface.

### 1. Process Executable & Directory Settings

| Property | Type | Description |
| :--- | :--- | :--- |
| `TargetFilePath` | `string` | Gets the file path of the executable, batch file, or script that the command runs. |
| `Arguments` | `string` | Gets the command-line arguments passed to the underlying process. |
| `WorkingDirPath` | `string` | Gets the working directory path set for the underlying process. |

### 2. Execution Context & Security

| Property | Type | Description |
| :--- | :--- | :--- |
| `ResourcePolicy` | `ResourcePolicy` | Gets the resource policy set for the underlying process. |
| `Credentials` | `Credentials` | Gets the user credentials configured for running the process. |
| `EnvironmentVariables` | `IReadOnlyDictionary<string, string?>` | Gets a read-only dictionary of environment variables set for the process. Keys are variable names (`string`), and values are variable values (`string?`, which can be `null`). |

### 3. Stream Redirection Pipes

| Property | Type | Description |
| :--- | :--- | :--- |
| `StandardInputPipe` | `PipeSource` | Gets the `PipeSource` connected to the standard input (`stdin`) stream of the process. |
| `StandardOutputPipe` | `PipeTarget` | Gets the `PipeTarget` connected to the standard output (`stdout`) stream of the process. |
| `StandardErrorPipe` | `PipeTarget` | Gets the `PipeTarget` connected to the standard error (`stderr`) stream of the process. |

### 4. Validation

| Property | Type | Description |
| :--- | :--- | :--- |
| `Validation` | `CommandResultValidation` | Gets the strategy used to validate the result of the process execution. |

---

## Key Components & Referenced Types

The interface relies on several custom types within the `CliWrap` ecosystem:

* **`ResourcePolicy`**: Defines policy rules governing resource usage for the execution context.
* **`Credentials`**: Represents identity details (e.g., username, password, domain) used to execute the underlying process.
* **`CommandResultValidation`**: An enumeration or type specifying how command exit codes or execution results should be evaluated.
* **`PipeSource`**: Represents the source of data piped into the command's standard input stream.
* **`PipeTarget`**: Represents the target destination receiving data from the command's standard output or standard error streams.