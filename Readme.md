# CliWrap

[![Build](https://github.com/Tyrrrz/CliWrap/workflows/CI/badge.svg?branch=master)](https://github.com/Tyrrrz/CliWrap/actions)
[![Coverage](https://codecov.io/gh/Tyrrrz/CliWrap/branch/master/graph/badge.svg)](https://codecov.io/gh/Tyrrrz/CliWrap)
[![Version](https://img.shields.io/nuget/v/CliWrap.svg)](https://nuget.org/packages/CliWrap)
[![Downloads](https://img.shields.io/nuget/dt/CliWrap.svg)](https://nuget.org/packages/CliWrap)
[![Donate](https://img.shields.io/badge/donate-$$$-purple.svg)](https://tyrrrz.me/donate)

CliWrap is a library for interacting with command line executables in a functional manner. It provides a convenient model for launching external processes, redirecting inputs and outputs, awaiting completion, and handling cancellation. At its core, it's based on a very robust piping model that lets you create intricate execution setups with minimal effort.

## Download

- [NuGet](https://nuget.org/packages/CliWrap): `dotnet add package CliWrap`

## Features

- Airtight abstraction over `System.Diagnostics.Process`
- Fluent interface for configuring commands
- Flexible support for piping with variety of sources and targets
- Fully asynchronous and cancellation-aware API
- Safety against typical deadlock scenarios
- Tested on Windows, Linux, and macOS
- Works with .NET Standard 2.0+, .NET Core 2.0+, .NET Framework 4.6.1+
- No external dependencies

## Usage

_Looking for documentation for CliWrap v2.5? You [can find it here](https://github.com/Tyrrrz/CliWrap/blob/43a93e37c81d8dda9c96343c6c7fb160933415ac/Readme.md). For v2.5 to v3.0 migration guide [check out the wiki](https://github.com/Tyrrrz/CliWrap/wiki/Migration-guide-(from-v2.5-to-v3.0))._

- [Executing a command](#executing-a-command)
- [Executing a command with buffering](#executing-a-command-with-buffering)
- [Configuring arguments and other options](#configuring-arguments-and-other-options)
- [Executing a command as an event stream](#executing-a-command-as-an-event-stream)
- [Piping](#piping)
- [Timeout and cancellation](#timeout-and-cancellation)
- [Getting process ID of an executing command](#getting-process-id-of-an-executing-command)
- [Lazily mapping the result of an execution](#lazily-mapping-the-result-of-an-execution)

### Executing a command

The following is a basic example that shows how to execute a command with the specified command line arguments:

```csharp
using CliWrap;

var result = await Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .ExecuteAsync();

// Result contains:
// -- result.ExitCode        (int)
// -- result.StartTime       (DateTimeOffset)
// -- result.ExitTime        (DateTimeOffset)
// -- result.RunTime         (TimeSpan)
```

The code above spawns a child process and then asynchronously waits for it to exit. After the task is completed, you get a `CommandResult` object that contains the exit code and runtime information.

By default, CliWrap pipes stdin, stdout, stderr to its own equivalent of `/dev/null`, to avoid potential deadlocks and unnecessary memory allocations. This is fine for typical use cases where you just want to run a command and don't really care about what it writes to the console.

> Note: unless configured otherwise, `ExecuteAsync()` throws a `CommandExecutionException` if the underlying process returns a non-zero exit code, which usually indicates an execution failure. You can choose to [disable this check](#configuring-arguments-and-other-options) if you want.

Command execution is completely stateless (as far as CliWrap is concerned) and can be repeated as many times as needed. Every call to `ExecuteAsync()` spawns a new process:

```csharp
var cmd = Cli.Wrap("path/to/exe").WithArguments("--foo bar");

// Run the command 5 times in a row
for (var i = 0; i < 5; i++)
    await cmd.ExecuteAsync();
```

### Executing a command with buffering

Often times a command may write some useful information to the console that you'll want to extract. There are different ways to do that with CliWrap. -- one of them is to execute the command with buffering:

```csharp
using CliWrap;
using CliWrap.Buffered;

var result = await Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .ExecuteBufferedAsync();

// Result contains:
// -- result.StandardOutput  (string)
// -- result.StandardError   (string)
// -- result.ExitCode        (int)
// -- result.StartTime       (DateTimeOffset)
// -- result.ExitTime        (DateTimeOffset)
// -- result.RunTime         (TimeSpan)
```

This execution model is very similar to `ExecuteAsync()`, but in this case the stdout and stderr streams are buffered in-memory. You can see what the console wrote to those streams by inspecting the `StandardOutput` and `StandardError` properties on the returned `BufferedCommandResult` object.

By default, `ExecuteBufferedAsync()` will assume that the underlying command uses `Console.OutputEncoding` for writing text to the console. If you need to, it's also possible to override encoding using one of the overloads:

```csharp
// Treat both stdout and stderr as UTF8-encoded text streams
var result = await Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .ExecuteBufferedAsync(Encoding.UTF8);

// Treat stdout as ASCII-encoded and stderr as UTF8-encoded
var result = await Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .ExecuteBufferedAsync(Encoding.ASCII, Encoding.UTF8);
```

This approach works really well when you want to run a command and also get the output. However, be careful as some CLIs may produce a lot of output and it might be inefficient to store it all in-memory. Additionally, stdout and stderr streams are not necessarily limited to text and can potentially contain binary data. In those more complicated scenarios, it's better to build commands using CliWrap's [piping](#piping) feature instead.

### Configuring arguments and other options

With the help of CliWrap's fluent interface and many overloads, it's really easy to configure various options related to command execution. For example, here are the different ways you can specify command line arguments:

```csharp
// Set arguments directly (no formatting, no escaping)
var command = Cli.Wrap("git")
    .WithArguments("clone https://github.com/Tyrrrz/CliWrap --depth 10");

// Set arguments from a list (joined to a string, with escaping)
var command = Cli.Wrap("git")
    .WithArguments(new[] {"clone", "https://github.com/Tyrrrz/CliWrap", "--depth", "10"});

// Build arguments from parts (joined to a string, with escaping & formatting)
var command = Cli.Wrap("git")
    .WithArguments(a => a
        .Add("clone")
        .Add("https://github.com/Tyrrrz/CliWrap")
        .Add("--depth")
        .Add(10));
```

While all of these approaches can be used interchangeably, the last two take care of escaping automatically for you, which is useful as you don't have to worry about spaces and other special characters. Moreover, the builder approach can automatically format values like `int` or `double` so you don't have to convert them yourself.

Besides command line arguments, you can also configure other aspects, such as environment variables and working directory:

```csharp
var command = Cli.Wrap("git")
    .WithWorkingDirectory("path/to/repo/")
    .WithArguments(a => a
        .Add("commit")
        .Add("-m")
        .Add("my commit"))
    .WithEnvironmentVariables(e => e
        .Set("GIT_AUTHOR_NAME", "John")
        .Set("GIT_AUTHOR_EMAIL", "john@email.com"));
```

Additionally, you can call `WithValidation()` to configure whether the command will throw an exception in case the execution finishes with a non-zero exit code:

```csharp
var commandNoCheck = Cli.Wrap("git").WithValidation(CommandResultValidation.None);
var commandWithCheck = Cli.Wrap("git").WithValidation(CommandResultValidation.ZeroExitCode); // (default)
```

CliWrap is designed with full immutability in mind -- all methods that return `Command` create a new instance instead of mutating an existing one. This means that you can safely re-use parts of your commands if necessary:

```csharp
var pullCmd = Cli.Wrap("git")
    .WithWorkingDirectory("path/to/repo/")
    .WithArguments("pull")
    .WithEnvironmentVariables(e => e
        .Set("GIT_AUTHOR_NAME", "John")
        .Set("GIT_AUTHOR_EMAIL", "john@email.com"));

var pushCmd = command1.WithArguments("push");
```

In the above example, `pullCmd` and `pushCmd` are separate objects. They have the same target executable, working directory and environment variables, but different command line arguments.

### Executing a command as an event stream

Besides executing a command as a task, CliWrap also supports an alternative model, in which an execution is represented as an event stream.

This lets you start a command and react to events it produces as it run. These events are:

- `StartedCommandEvent` -- received just once, when the command starts executing. Contains the process ID.
- `StandardOutputCommandEvent` -- received every time the underlying process writes a new line to the output stream. Contains the text as string.
- `StandardErrorCommandEvent` -- received every time the underlying process writes a new line to the error stream. Contains the text as string.
- `ExitedCommandEvent` -- received just once, when the command finishes executing. Contains the exit code.

There are two ways you can start a command and listen to its events. One of them is through an asynchronous pull-based stream:

```csharp
using CliWrap;
using CliWrap.EventStream;

var cmd = Cli.Wrap("foo").WithArguments("bar");

await foreach (var cmdEvent in cmd.ListenAsync())
{
    switch (cmdEvent)
    {
        case StartedCommandEvent started:
            _output.WriteLine($"Process started; ID: {started.ProcessId}");
            break;
        case StandardOutputCommandEvent stdOut:
            _output.WriteLine($"Out> {stdOut.Text}");
            break;
        case StandardErrorCommandEvent stdErr:
            _output.WriteLine($"Err> {stdErr.Text}");
            break;
        case ExitedCommandEvent exited:
            _output.WriteLine($"Process exited; Code: {exited.ExitCode}");
            break;
    }
}
```

The `ListenAsync()` method starts the command and returns an object of type `IAsyncEnumerable<CommandEvent>`, which you can iterate over using the `await foreach` construct introduced in C# 8. In this scenario, back-pressure is performed by locking the pipes until an event is processed, which means there's no buffering of data in memory.

Alternatively, you can also start a command as an observable push-based stream instead:

```csharp
using CliWrap;
using CliWrap.EventStream;
using System.Reactive;

await cmd.Observe().ForEachAsync(cmdEvent =>
{
    switch (cmdEvent)
    {
        case StartedCommandEvent started:
            _output.WriteLine($"Process started; ID: {started.ProcessId}");
            break;
        case StandardOutputCommandEvent stdOut:
            _output.WriteLine($"Out> {stdOut.Text}");
            break;
        case StandardErrorCommandEvent stdErr:
            _output.WriteLine($"Err> {stdErr.Text}");
            break;
        case ExitedCommandEvent exited:
            _output.WriteLine($"Process exited; Code: {exited.ExitCode}");
            break;
    }
});
```

In this case, `Observe()` returns a cold `IObservable<CommandEvent>` that represents the command execution. You can use the set of extensions provided by [Rx.NET](https://github.com/dotnet/reactive) to transform, filter, throttle, and otherwise manipulate the stream. There is no locking in this scenario so the data is pushed at the rate it becomes available.

Similarly to `ExecuteBufferedAsync()`, both `ListenAsync()` and `Observe()` have overloads that accept custom encoding.

### Piping

Most of the features you've seen so far are based on CliWrap's core model of piping. In fact, `ExecuteBufferedAsync()`, `ListenAsync()`, and `Observe()` are all just extension methods that wrap around piped commands.

The piping feature lets you redirect input and output streams of the underlying process to form a complex execution pipeline. To facilitate that, the `Command` object has three methods:

- `WithStandardInputPipe(PipeSource source)`
- `WithStandardOutputPipe(PipeTarget target)`
- `WithStandardErrorPipe(PipeTarget target)`

By default, every command is piped from `PipeSource.Null` and is itself piped to `PipeTarget.Null`, which are CliWrap's equivalents of `/dev/null`. You can change that and, for example, have the command pipe its standard input from one file and redirect its standard output to another:

```csharp
await using var input = File.OpenRead("input.txt");
await using var output = File.Create("output.txt");

await Cli.Wrap("foo")
    .WithStandardInputPipe(PipeSource.FromStream(input))
    .WithStandardOutputPipe(PipeTarget.ToStream(output))
    .ExecuteAsync();
```

The exact same thing can be expressed in a terser way using pipe operators:

```csharp
await using var input = File.OpenRead("input.txt");
await using var output = File.Create("output.txt");

await (input | Cli.Wrap("foo") | output).ExecuteAsync();
```

Just like with other configuration methods, redirecting pipes results in a new immutable object. This protects you from accidentally reusing the same pipes in different commands, as that usually leads to really confusing errors.

Besides raw streams, `PipeSource` and `PipeTarget` both have factory methods that let you create different piping abstractions:

- `PipeSource.Null` -- represents an empty pipe source.
- `PipeSource.FromStream()` -- pipes data from any readable stream.
- `PipeSource.FromBytes()` -- pipes data from a byte array.
- `PipeSource.FromString()` -- pipes from a text string (supports custom encoding)
- `PipeSource.FromCommand()` -- pipes data from standard output of another command.
- `PipeTarget.Null` -- represents a pipe target that discards all data.
- `PipeTarget.ToStream()` -- pipes data into any writeable stream.
- `PipeTarget.ToStringBuilder()` -- pipes data as text into `StringBuilder` (supports custom encoding).
- `PipeTarget.ToDelegate()` -- pipes data as text, line-by-line, into `Action<string>` or `Func<string, Task>` (supports custom encoding).
- `PipeTarget.Merge()` -- merges multiple outbound pipes into one.

The pipe operator also has overloads for most of these. Below you can see some examples of what you can do with the piping feature that CliWrap provides.

Pipe a string into stdin:

```csharp
var cmd = "Hello world" | Cli.Wrap("foo");
await cmd.ExecuteAsync();
```

Pipe stdout as text into a `StringBuilder`:

```csharp
var stdOutBuffer = new StringBuilder();

var cmd = Cli.Wrap("foo") | stdOutBuffer;
await cmd.ExecuteAsync();
```

Pipe a binary HTTP stream into stdin:

```csharp
using var httpClient = new HttpClient();
await using var input = await httpClient.GetStreamAsync("https://example.com/image.png");

var cmd = input | Cli.Wrap("foo");
await cmd.ExecuteAsync();
```

Pipe stdout of one command into stdin of another:

```csharp
var cmd = Cli.Wrap("foo") | Cli.Wrap("bar") | Cli.Wrap("baz");
await cmd.ExecuteAsync();
```

Pipe stdout and stderr into those of parent process:

```csharp
await using var stdOut = Console.OpenStandardOutput();
await using var stdErr = Console.OpenStandardError();

var cmd = Cli.Wrap("foo") | (stdOut, stdErr);
await cmd.ExecuteAsync();
```

Pipe stdout to a delegate:

```csharp
var cmd = Cli.Wrap("foo") | Debug.WriteLine;
await cmd.ExecuteAsync();
```

Pipe stdout into a file and stderr into a `StringBuilder`:

```csharp
await using var file = File.Create("output.txt");
var buffer = new StringBuilder();

var cmd = Cli.Wrap("foo") |
    (PipeTarget.ToStream(file), PipeTarget.ToStringBuilder(buffer));

await cmd.ExecuteAsync();
```

Pipe stdout into multiple files simultaneously:

```csharp
await using var file1 = File.Create("file1.txt");
await using var file2 = File.Create("file2.txt");
await using var file3 = File.Create("file3.txt");

var target = PipeTarget.Merge(
    PipeTarget.ToStream(file1),
    PipeTarget.ToStream(file2),
    PipeTarget.ToStream(file3)
);

var cmd = Cli.Wrap("foo") | target;
await cmd.ExecuteAsync();
```

Pipe a string into a command, that command into another command, and then into parent's stdout and stderr:

```csharp
var cmd = "Hello world" | Cli.Wrap("foo")
    .WithArguments("print random") | Cli.Wrap("bar")
    .WithArguments("reverse") | (Console.WriteLine, Console.Error.WriteLine);

await cmd.ExecuteAsync();
```

As you can see, piping enables a wide range of different use cases. It's not only used for convenience, but also to improve memory efficiency when dealing with large and/or binary inputs and outputs. With the help of CliWrap's pipe operators, configuring pipelines is really easy -- just imagine doing the same with `System.Diagnostics.Process` manually.

Of course, if you're not comfortable using pipe operators, you can do everything using the `WithStandardXyzPipe()` methods instead. They work entirely the same.

The different execution models which we saw earlier, `ExecuteBufferedAsync()`, `ListenAsync()` and `Observe()` are all based on the concept of piping, but these approaches are not mutually exclusive. For example, you can create a piped command and start it as an event stream:

```csharp
await using var input = File.OpenRead("input.txt");
await using var output = File.Create("output.txt");

var cmd = input | Cli.Wrap("foo") | output;

await foreach (var cmdEvent in cmd.ListenAsync())
{
    // ...
}
```

This works because internally these methods call `PipeTarget.Merge()` to combine new pipes with those configured earlier.

### Timeout and cancellation

Command execution is asynchronous by nature because it involves a completely separate process. Often you may want to implement an abortion mechanism to stop the execution before it finishes, either by a manual trigger or a timeout.

To do that with CliWrap, you simply need to pass a `CancellationToken` that represents the cancellation signal:

```csharp
using var cts = new CancellationTokenSource();

// Cancel automatically after a timeout of 10 seconds
cts.CancelAfter(TimeSpan.FromSeconds(10));

var result = await Cli.Wrap("path/to/exe").ExecuteAsync(cts.Token);
```

When an execution is canceled, the underlying process is killed and the `ExecuteAsync()` method throws an exception of type `OperationCanceledException` (or its derivative, `TaskCanceledException`). You will have to catch this exception to recover from cancellation.

All execution models support cancellation -- `ExecuteAsync()`, `ExecuteBufferedAsync()`, `ListenAsync()`, `Observe()` can accept a cancellation token.

### Getting process ID of an executing command

The promise returned by `ExecuteAsync()` and `ExecuteBufferedAsync()` is in fact not `Task<T>` but `CommandTask<T>`. It's a special object that similarly can be awaited, but it also contains information about the ongoing execution.

You can inspect the task object and get the ID of the underlying process that is represented by this command execution:

```csharp
var task = Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .ExecuteAsync();

var processId = task.ProcessId;

await task;
```

### Lazily mapping the result of an execution

Additionally, you can transform the result of `CommandTask<T>` lazily with the help of `Select()` method:

```csharp
// We're only interested in the exit code
var exitCode = await Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .ExecuteAsync()
    .Select(r => r.ExitCode);

// We're only interested in stdout
var stdOut = await Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .ExecuteBufferedAsync()
    .Select(r => r.StandardOutput);
```
