# Technical Documentation: `EchoCommand.cs`

**File Path:** `CliWrap.Tests.Dummy/Commands/EchoCommand.cs`

---

## Overview

The `EchoCommand` class is a command component within the dummy test application for CliWrap. Built using the **CliFx** framework, its purpose is to simulate a standard standard CLI `echo` utility. It accepts a list of string items, joins them using a specified separator, and outputs the resulting string to designated output streams (such as standard output or standard error).

---

## Class Header & Metadata

```csharp
namespace CliWrap.Tests.Dummy.Commands;

[Command("echo")]
public partial class EchoCommand : ICommand
```

* **Namespace:** `CliWrap.Tests.Dummy.Commands`
* **Command Attribute:** `[Command("echo")]` maps this class to the `echo` CLI command line identifier.
* **Interface:** Implements `CliFx.ICommand`, defining it as an executable command within the CliFx ecosystem.

---

## Properties / Inputs

### 1. `Items`
```csharp
[CommandParameter(0)]
public required IReadOnlyList<string> Items { get; set; }
```
* **Type:** `IReadOnlyList<string>`
* **Attribute:** `[CommandParameter(0)]`
* **Description:** Represents a positional parameter at index `0`. It takes a list of text items supplied to the command. It is marked with the `required` modifier.

---

### 2. `Target`
```csharp
[CommandOption("target")]
public OutputTarget Target { get; set; } = OutputTarget.StdOut;
```
* **Type:** `OutputTarget` (imported from `CliWrap.Tests.Dummy.Commands.Shared`)
* **Attribute:** `[CommandOption("target")]`
* **Description:** Specifies the destination target output stream for the command output.
* **Default Value:** `OutputTarget.StdOut`

---

### 3. `Separator`
```csharp
[CommandOption("separator")]
public string Separator { get; set; } = " ";
```
* **Type:** `string`
* **Attribute:** `[CommandOption("separator")]`
* **Description:** Specifies the delimiter string used to join the individual strings in `Items`.
* **Default Value:** `" "` (a single space)

---

## Command Execution Logic

### `ExecuteAsync(IConsole console)`

```csharp
public async ValueTask ExecuteAsync(IConsole console)
{
    foreach (var writer in console.GetWriters(Target))
        await writer.WriteLineAsync(string.Join(Separator, Items));
}
```

### Execution Steps:
1. Receives an `IConsole` abstraction instance from CliFx.
2. Calls the extension/helper method `console.GetWriters(Target)` to retrieve output stream writers corresponding to the configured `Target`.
3. Concatenates all elements in the `Items` collection using `string.Join(Separator, Items)`.
4. Asynchronously writes the resulting combined string followed by a line terminator to each writer using `await writer.WriteLineAsync(...)`.

---

## Dependencies

* `System.Collections.Generic`
* `System.Threading.Tasks`
* `CliFx`
* `CliFx.Binding`
* `CliFx.Infrastructure`
* `CliWrap.Tests.Dummy.Commands.Shared`