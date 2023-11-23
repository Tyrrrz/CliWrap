# CliWrap.Magic

[![Version](https://img.shields.io/nuget/v/CliWrap.Magic.svg)](https://nuget.org/packages/CliWrap.Magic)
[![Downloads](https://img.shields.io/nuget/dt/CliWrap.Magic.svg)](https://nuget.org/packages/CliWrap.Magic)

**CliWrap.Magic** is an extension package for **CliWrap** that provides a shell-like experience for executing commands.

## Install

- 📦 [NuGet](https://nuget.org/packages/CliWrap.Magic): `dotnet add package CliWrap.Magic`

## Usage

**CliWrap.Magic** provides a static `Shell` class that contains a various methods for creating and executing commands.
The recommended way to use it is by statically importing the class:

```csharp
using CliWrap.Magic;
using static CliWrap.Magic.Shell;

// ...
```

### Executing commands

In order to run a command with **CliWrap.Magic**, use the `_` method with the target file path:

```csharp
using CliWrap.Magic;
using static CliWrap.Magic.Shell;

await _("dotnet");
var version = await _("dotnet", "--version");
```

Piping works the same way as it does in regular **CliWrap**:

```csharp
await ("standard input" | _("dotnet", "run"));
```

### Tools