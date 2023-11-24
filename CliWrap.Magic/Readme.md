# CliWrap.Magic

[![Version](https://img.shields.io/nuget/v/CliWrap.Magic.svg)](https://nuget.org/packages/CliWrap.Magic)
[![Downloads](https://img.shields.io/nuget/dt/CliWrap.Magic.svg)](https://nuget.org/packages/CliWrap.Magic)

**CliWrap.Magic** is an extension package for **CliWrap** that provides a shell-like experience for executing commands.

## Install

- 📦 [NuGet](https://nuget.org/packages/CliWrap.Magic): `dotnet add package CliWrap.Magic`

## Usage

### Quick overview

Add `using static CliWrap.Magic.Spells;` to your file and start writing scripts like this:

```csharp
using static CliWrap.Magic.Spells;

// Create commands using the _() method, execute them simply by awaiting.
// Check for exit code directly in if statements.
if (!await _("git"))
{
    WriteErrorLine("Git is not installed");
    Exit(1);
    return;
}

// Executing a command returns an object which has implicit conversions to:
// - int (exit code)
// - bool (exit code == 0)
// - string (standard output)
string version = await _("git", "--version"); // git version 2.43.0.windows.1
WriteLine($"Git version: {version}");

// Just like with regular CliWrap, arguments are automatically
// escaped to form a well-formed command line string.
// Non-string arguments of many different types can also be passed directly.
await _("git", "clone", "https://github.com/Tyrrrz/CliWrap", "--depth", 0);

// Resolve environment variables easily with the Environment() method.
var commit = Environment("HEAD_SHA");

// Prompt the user for additional input with the ReadLine() method.
// Check for truthy values using the IsTruthy() method.
if (!IsTruthy(commit))
    commit = ReadLine("Enter commit hash");

// Just like with regular CliWrap, arguments are automatically
// escaped to form a well-formed command line string.
await _("git", "checkout", commit);

// Set environment variables using the Environment() method.
// This returns an object that you can dispose to restore the original value.
using (Environment("HEAD_SHA", "deadbeef"))
{
    await _("/bin/sh", "-c", "echo $HEAD_SHA"); // deadbeef
    
    // You can also run commands in the default system shell directly
    // using the Shell() method.
    await Shell("echo $HEAD_SHA"); // deadbeef
}

// Same with the WorkingDirectory() method.
using (WorkingDirectory("/tmp/my-script/"))
{
    // Get the current working directory using the same method.
    var cwd = WorkingDirectory();
}

// Magic also supports CliWrap's piping syntax.
var commits = new List<string>(); // this will contain commit hashes
await (
    _("git", "log", "--pretty=format:%H") | commits.Add
);
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