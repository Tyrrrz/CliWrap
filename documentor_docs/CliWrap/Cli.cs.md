# Technical Documentation: `CliWrap/Cli.cs`

## Overview

The `CliWrap/Cli.cs` file defines the primary static entry point for the `CliWrap` library. Its main purpose is to provide a clean, fluent syntax (`Cli.Wrap(...)`) for instantiating new command objects targeting executables, batch files, or scripts.

---

## Namespace

`namespace CliWrap;`

The `Cli` class belongs to the root `CliWrap` namespace.

---

## Class Definition

### `public static class Cli`

* **Type**: `public static class`
* **Purpose**: Serves as the main entry point for creating `Command` instances. 

---

## Static Methods

### `Wrap(string targetFilePath)`

Creates and returns a new `Command` instance targeting the specified executable, batch file, or script path.

#### Syntax
```csharp
public static Command Wrap(string targetFilePath)
```

#### Parameters

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `targetFilePath` | `string` | The path to the command-line executable, batch file, or script to be executed. |

#### Return Value

* **Type**: `Command`
* **Description**: A new instance of the `Command` class initialized with the provided `targetFilePath`.

---

## How It Works

The `Cli` class acts as a simple factory wrapper. When `Cli.Wrap(targetFilePath)` is called:
1. It accepts the `targetFilePath` string representing the target binary or script.
2. It calls the `Command` constructor (`new(targetFilePath)`), passing the path argument.
3. It returns the newly created `Command` object to the caller.