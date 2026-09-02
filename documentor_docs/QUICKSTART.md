# Quickstart Guide

> **Note**: Specific package installation commands (e.g., NuGet/CLI installation instructions) and system prerequisites are not explicitly defined in the provided context. The setup and usage below are derived strictly from the library's C# configuration API and test specifications.

---

## Creating and Configuring Commands

Commands are initialized using `Cli.Wrap(...)` and can be configured with various options before execution.

### Default Command Setup

By default, a command is configured with standard defaults (current working directory, zero exit code validation, null pipes, and no environment variables or credentials):

```csharp
var cmd = Cli.Wrap("target_file");
```

---

## Configuration Options

### 1. Executable / Target File
Set or update the target executable path:

```csharp
var cmd = Cli.Wrap("foo").WithTargetFile("bar");
```

### 2. Command Line Arguments
Arguments can be configured as a string, an array of strings, or via a builder action:

```csharp
// Using a string
var cmd = Cli.Wrap("foo").WithArguments("qqq ppp");

// Using an array
var cmd = Cli.Wrap("foo").WithArguments(["-a", "foo bar"]);

// Using a builder
var cmd = Cli.Wrap("foo").WithArguments(b => b
    .Add("-a")
    .Add("foo bar")
    .Add(3.14)
    .Add(["foo", "bar"])
);
```

### 3. Working Directory
Set the working directory path for the process:

```csharp
var cmd = Cli.Wrap("foo").WithWorkingDirectory("path/to/directory");
```

### 4. Environment Variables
Configure environment variables using standard dictionaries or a fluent builder delegate. Setting a variable to `null` unsets it.

```csharp
// Using a Dictionary
var env = new Dictionary<string, string?> 
{ 
    ["name"] = "value", 
    ["key"] = "door" 
};
var cmd = Cli.Wrap("foo").WithEnvironmentVariables(env);

// Using a Builder
var cmd = Cli.Wrap("foo").WithEnvironmentVariables(b => b
    .Set("name", "value")
    .Set("key", "door")
    .Set("variableToUnset", null)
);
```

### 5. Resource Policy
Configure priority class, affinity, or working set sizes:

```csharp
// Direct instance
var policy = new ResourcePolicy(ProcessPriorityClass.High, 0x1, 1024, 2048);
var cmd = Cli.Wrap("foo").WithResourcePolicy(policy);

// Using a builder
var cmd = Cli.Wrap("foo").WithResourcePolicy(b => b
    .SetPriority(ProcessPriorityClass.High)
    .SetAffinity(0x1)
    .SetMinWorkingSet(1024)
    .SetMaxWorkingSet(2048)
);
```

### 6. User Credentials
Set process execution credentials:

```csharp
// Direct instance
var credentials = new Credentials("domain", "username", "password", true);
var cmd = Cli.Wrap("foo").WithCredentials(credentials);

// Using a builder
var cmd = Cli.Wrap("foo").WithCredentials(c => c
    .SetDomain("domain")
    .SetUserName("username")
    .SetPassword("password")
    .LoadUserProfile()
);
```

### 7. Result Validation Strategy
Choose how command execution results are validated:

```csharp
// Require zero exit code (Default)
var cmd = Cli.Wrap("foo").WithValidation(CommandResultValidation.ZeroExitCode);

// Disable validation
var cmd = Cli.Wrap("foo").WithValidation(CommandResultValidation.None);
```

### 8. Standard I/O Pipes
Configure input, output, and error redirection:

```csharp
var cmd = Cli.Wrap("foo")
    .WithStandardInputPipe(PipeSource.FromString("input data"))
    .WithStandardOutputPipe(PipeTarget.ToStream(Stream.Null))
    .WithStandardErrorPipe(PipeTarget.ToStream(Stream.Null));
```

---

## Command Execution Example

To execute a configured command asynchronously and capture buffered output:

```csharp
var cmd = Cli.Wrap("executable")
    .WithArguments("cwd")
    .WithWorkingDirectory("path/to/dir")
    .WithEnvironmentVariables(new Dictionary<string, string?> { ["foo"] = "bar" });

// Execute asynchronously
var result = await cmd.ExecuteBufferedAsync();

// Access output
string output = result.StandardOutput;
```