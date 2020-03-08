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
- Targets .NET Standard 2.0+, .NET Core 2.0+, .NET Framework 4.6.1+
- No external dependencies

## Usage

_Looking for documentation for CliWrap v2.5? You [can find it here](https://github.com/Tyrrrz/CliWrap/blob/43a93e37c81d8dda9c96343c6c7fb160933415ac/Readme.md). For v2.5 to v3.0 migration guide [check out the wiki](https://github.com/Tyrrrz/CliWrap/wiki/Migration-guide-(from-v2.5-to-v3.0))._

### Executing a command

The following is a basic example that shows how to asynchronously execute a command by specifying command line arguments:

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

In this scenario, all of the streams are redirected into CliWrap's equivalent of `/dev/null`, avoiding unnecessary memory allocations. This approach is useful if you want to execute a command, but don't care about what it writes to the console. You still get the returned exit code, which is usually enough to determine whether the command has run successfully or not.

By default, `ExecuteAsync()` will throw a `CommandExecutionException` if the underlying process returned a non-zero exit code. You can choose to [disable this check](#configuring-arguments-and-other-options).

Command configuration and execution are not coupled in any way so you can separate them:

```csharp
var cmd = Cli.Wrap("path/to/exe").WithArguments("--foo bar");
var result = await cmd.ExecuteAsync();
```

Every call to `ExecuteAsync()` is stateless and will spawn a new process for each execution.

### Executing a command with buffering

You can also execute a command while buffering its outputs in memory:

```csharp
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

Calling `ExecuteBufferedAsync()` is similar to `ExecuteAsync()`, but the returned result contains two extra fields: `StandardOutput` and `StandardError`. These contain the aggregated text data produced by the underlying command. This approach is useful if you want to execute a command and then inspect what it wrote to the console. Note, however, that some commands may produce really large outputs or even write binary content instead of text, in which case it's better to use [direct piping methods](#piping).

By default, this method will assume that the underlying command uses `Console.OutputEncoding` for writing text to the console. If it doesn't, you can override it using one of the overloads:

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

### Getting process ID

The promise returned by `ExecuteAsync()` and `ExecuteBufferedAsync()` is in fact not `Task<T>` but `CommandTask<T>`. It's a special object that similarly can be awaited, but on top of that it contains information about the ongoing execution.

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

### Configuring arguments and other options

CliWrap has a fluent interface with various overloads to help configure different options related to command execution. For example, here are three alternative ways you can configure command line arguments:

```csharp
// Set arguments directly (no formatting, no escaping)
var command = Cli.Wrap("git")
    .WithArguments("clone https://github.com/Tyrrrz/CliWrap --depth 10");

// Set arguments from a list (joined to a string, with escaping)
var command = Cli.Wrap("git")
    .WithArguments(new[] {"clone", "https://github.com/Tyrrrz/CliWrap", "--depth", "10"});

// Build arguments from parts (joined to a string, with formatting and escaping)
var command = Cli.Wrap("git")
    .WithArguments(a => a
        .Add("clone")
        .Add("https://github.com/Tyrrrz/CliWrap")
        .Add("--depth")
        .Add(10));
```

While all of these approaches can be used interchangeably, the last two take care of escaping automatically for you. Moreover, the builder approach has some overloads to help with formatting, which makes it the preferred approach in most situations.

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

Additionally, you can use `WithValidation()` to configure whether the command will throw an exception in case the execution finishes with a non-zero exit code:

```csharp
var commandNoCheck = Cli.Wrap("git").WithValidation(CommandResultValidation.None);
var commandWithCheck = Cli.Wrap("git").WithValidation(CommandResultValidation.ZeroExitCode); // (default)
```

Note that each call to any of these mentioned `WithXyz()` methods returns a completely new immutable object, with the corresponding property set to the specified value. That means you can safely re-use parts of your commands as you see fit:

```csharp
var command1 = Cli.Wrap("git")
    .WithWorkingDirectory("path/to/repo/")
    .WithArguments("--version")
    .WithEnvironmentVariables(e => e
        .Set("GIT_AUTHOR_NAME", "John")
        .Set("GIT_AUTHOR_EMAIL", "john@email.com"));

var command2 = command1.WithArguments("pull");
```

In the above example, `command1` and `command2` are separate objects, sharing all configuration options except command line arguments.

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

All other execution models like `ExecuteBufferedAsync()` similarly accept cancellation tokens as well.

### Executing a command as an event stream

Besides executing a command as a task, CliWrap also supports an alternative model, in which an execution is represented as an event stream. In this scenario, the underlying command may trigger the following events:

- `StartedCommandEvent` -- received just once, when the command starts executing. Contains the process ID.
- `StandardOutputCommandEvent` -- received every time the underlying process writes a new line to the output stream. Contains the text as string.
- `StandardErrorCommandEvent` -- received every time the underlying process writes a new line to the error stream. Contains the text as string.
- `ExitedCommandEvent` -- received just once, when the command successfully finishes executing. Contains the exit code.

There are two ways you can start a command and listen to its events. One of them is through an asynchronous pull-based stream:

```csharp
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

The `ListenAsync()` method starts the command and returns an object of type `IAsyncEnumerable<CommandEvent>`, which you can iterate over using the `await foreach` construct introduced with C# 8. In this scenario, back-pressure is performed by locking the pipes until an event is processed, which means there's no buffering of data in memory.

Alternatively, you can also start a command as an observable push-based stream instead:

```csharp
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

Both `ListenAsync()` and `Observe()` also have overloads that accept custom encoding and/or a cancellation token.

### Piping

Most of the features you've seen so far are based on CliWrap's core model of piping. It lets you redirect input and output streams of the underlying process to form a complex execution pipeline.

To facilitate piping, `Command` object has three methods:

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

Besides raw streams, `PipeSource` and `PipeTarget` both have factory methods that help express different piping directions:

- `PipeSource.Null` -- represents an empty pipe source.
- `PipeSource.FromStream()` -- pipes data from any readable stream.
- `PipeSource.FromBytes()` -- pipes data from a byte array.
- `PipeSource.FromString()` -- pipes from a text string (supports custom encoding)
- `PipeSource.FromCommand()` -- pipes data from standard output of another command.
- `PipeTarget.Null` -- represents a pipe target that discards all data.
- `PipeTarget.ToStream()` -- pipes data into any writeable stream.
- `PipeTarget.ToStringBuilder()` -- pipes data as text into `StringBuilder` (supports custom encoding).
- `PipeTarget.ToDelegate()` -- pipes data as text, line-by-line, into `Action<string>` or `Func<string, Task>` (supports custom encoding).
- `PipeTarget.Merge()` -- merges multiple pipes into one.

The pipe operator also has overloads for most of these. Below you can see some examples of what you can do.

Pipe a string into stdin:

```csharp
await ("Hello world" | Cli.Wrap("foo")).ExecuteAsync();
```

Pipe stdout as text into a `StringBuilder`:

```csharp
var stdOutBuffer = new StringBuilder();
await (Cli.Wrap("foo") | stdOutBuffer).ExecuteAsync();
```

Pipe a binary HTTP stream into stdin:

```csharp
using var httpClient = new HttpClient();
await using var input = await httpClient.GetStreamAsync("https://example.com/image.png");

await (input | Cli.Wrap("foo")).ExecuteAsync();
```

Pipe stdout of one command into stdin of another:

```csharp
await (Cli.Wrap("foo") | Cli.Wrap("bar") | Cli.Wrap("baz")).ExecuteAsync();
```

Pipe stdout and stderr into those of parent process:

```csharp
await using var stdOut = Console.OpenStandardOutput();
await using var stdErr = Console.OpenStandardError();

var cmd = Cli.Wrap("foo") | (stdOut, stdErr);
await cmd.ExecuteAsync();
```

```csharp
var cmd = Cli.Wrap("foo") |
    (Console.WriteLine, Console.Error.WriteLine);

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
    PipeTarget.ToStream(file1)
    PipeTarget.ToStream(file2)
    PipeTarget.ToStream(file3));

await (Cli.Wrap("foo") | target).ExecuteAsync();
```

Pipe a string into a command, that command into another command, and then into parent's stdout and stderr:

```csharp
var cmd = "Hello world" | Cli.Wrap("foo")
    .WithArguments("print random") | Cli.Wrap("bar")
    .WithArguments("reverse") | (Console.WriteLine, Console.Error.WriteLine);

await cmd.ExecuteAsync();
```

As you can see, piping enables a wide range of different use cases. It's not only used for convenience, but also to improve memory efficiency when dealing with large and/or binary inputs and outputs. With the help of CliWrap's pipe operators, configuring pipelines is really easy -- just imagine doing the same with `System.Diagnostics.Process` manually.

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

This works because internally these methods call `PipeTarget.Merge` to add additional pipe targets while preserving those configured earlier.