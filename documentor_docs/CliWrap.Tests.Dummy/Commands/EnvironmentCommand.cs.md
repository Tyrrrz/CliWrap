# EnvironmentCommand Technical Documentation

## Overview

The `EnvironmentCommand` class is a command component within the `CliWrap.Tests.Dummy` test executable. Built using the **CliFx** framework, its primary purpose is to print the values of one or more specified system environment variables to the standard output console.

* **File Path:** `CliWrap.Tests.Dummy/Commands/EnvironmentCommand.cs`
* **Namespace:** `CliWrap.Tests.Dummy.Commands`
* **CLI Command Name:** `env`

---

## Class Structure & Attributes

```csharp
[Command("env")]
public partial class EnvironmentCommand : ICommand
```

* **`[Command("env")]`**: Registers this class as a command named `env` within the CliFx command-line application structure.
* **`ICommand`**: Implements the CliFx `ICommand` interface, requiring the implementation of the `ExecuteAsync` method.

---

## Properties

### `Names`

```csharp
[CommandParameter(0)]
public IReadOnlyList<string> Names { get; set; } = [];
```

* **Attribute:** `[CommandParameter(0)]`
  * Maps to the first positional command-line parameter (index `0`).
  * Accepts multiple values corresponding to environment variable names.
* **Type:** `IReadOnlyList<string>`
* **Default Value:** Empty array/list (`[]`).
* **Purpose:** Stores the names of the environment variables that the command should inspect and output.

---

## Method Implementation

### `ExecuteAsync(IConsole console)`

```csharp
public async ValueTask ExecuteAsync(IConsole console)
```

#### Parameters
* **`console` (`IConsole`)**: An abstraction provided by CliFx representing the console context (providing access to stdout, stderr, stdin, etc.).

#### Return Type
* **`ValueTask`**: Represents an asynchronous completion task.

#### Execution Flow
1. **Iterate Input Names:** Iterates sequentially through each string `name` contained in the `Names` collection.
2. **Fetch Environment Variable:** Calls .NET standard framework method `Environment.GetEnvironmentVariable(name)` to retrieve the current process/system value for the specified variable name.
3. **Write Output:** Asynchronously writes the retrieved environment variable value (or `null`/empty string if unset) to the console output stream using `await console.Output.WriteLineAsync(...)`.

---

## Summary of Operation

When the `env` command is invoked:
1. CliFx binds any positional arguments provided at parameter index `0` into the `Names` list property.
2. The framework invokes `ExecuteAsync(console)`.
3. For every string in `Names`, `EnvironmentCommand` gets its matching system environment variable value and writes it on a new line to `console.Output`.