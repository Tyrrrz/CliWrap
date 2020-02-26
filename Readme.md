# CliWrap

[![Build](https://github.com/Tyrrrz/CliWrap/workflows/CI/badge.svg?branch=master)](https://github.com/Tyrrrz/CliWrap/actions)
[![Coverage](https://codecov.io/gh/Tyrrrz/CliWrap/branch/master/graph/badge.svg)](https://codecov.io/gh/Tyrrrz/CliWrap)
[![Version](https://img.shields.io/nuget/v/CliWrap.svg)](https://nuget.org/packages/CliWrap)
[![Downloads](https://img.shields.io/nuget/dt/CliWrap.svg)](https://nuget.org/packages/CliWrap)
[![Donate](https://img.shields.io/badge/donate-$$$-purple.svg)](https://tyrrrz.me/donate)

CliWrap is a library for interacting with command line executables in a functional fashion. It provides a convenient model for launching external processes, redirecting inputs and outputs, awaiting completion, and handling cancellation. At its core, it's based on a very robust piping model that lets you create intricate execution setups with minimal effort.

## Download

- [NuGet](https://nuget.org/packages/CliWrap): `dotnet add package CliWrap`

## Features

- Full abstraction over `System.Diagnostics.Process`
- Execute commands in a fully asynchronous fashion
- Specify arguments, environment variables, and other options in a fluent way
- Configure pipelines with a very high degree of flexibility
- Cancel execution at any point in time using `CancellationToken`
- Immutable objects all the way through
- Complete safety against typical deadlock traps
- Fully tested on Windows, Linux, and macOS
- Supports .NET Standard 2.0+, .NET Core 2.0+, .NET Framework 4.6.1+
- No external dependencies

## Usage

_Looking for documentation for CliWrap v2.5? You [can find it here](https://github.com/Tyrrrz/CliWrap/blob/43a93e37c81d8dda9c96343c6c7fb160933415ac/Readme.md). For v2.5 to v3.0 migration guide [check out the wiki](https://github.com/Tyrrrz/CliWrap/wiki/Migration-guide-(from-v2.5-to-v3.0\))._

### Executing a command with predefined arguments

The following example shows how to asynchronously execute a command by specifying command line arguments. In this scenario, all input and output streams are redirected to a programmatic equivalent of `/dev/null`, avoiding unnecessary allocations.

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

You can call `ExecuteAsync()` more than once, which will start a new execution each time. There is no state stored in the `Command` object itself.

### Executing a command and buffering its outputs

You can also use `ExecuteBufferedAsync()` extension method to execute the command in a buffered manner, where the outputs are read into memory and persisted as strings in the result.

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

In case you need to specify a custom encoding, you can do that using one of the overloads of `ExecuteBufferedAsync()`:

```csharp
// Treat both stdout and stderr as UTF8-encoded string streams
var result = await Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .ExecuteBufferedAsync(Encoding.UTF8);

// Treat stdout as ASCII-encoded and stderr as UTF8-encoded
var result = await Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .ExecuteBufferedAsync(Encoding.ASCII, Encoding.UTF8);
```

### Executing a command and getting runtime information

The promise returned by `ExecuteAsync()` and `ExecuteBufferedAsync()` is in fact not `Task<T>` but `CommandTask<T>`. It's a special task object that can be similarly awaited, but on top of that it also contains information about the ongoing execution.

```csharp
var task = await Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .ExecuteAsync();

// Get the process ID
var processId = task.ProcessId;

// Wait until the task finishes
await task;
```

### Lazily mapping the result of an execution

Additionally, you can transform the result of `CommandTask<T>` lazily with the help of `Select()` method. It works exactly like its LINQ equivalent.

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

CliWrap offers a very flexible fluent interface to configure various options related to command execution. For example, here are two alternative ways you can configure command line arguments:

```csharp
// Set arguments directly
var command = Cli.Wrap("git")
    .WithArguments("clone https://github.com/Tyrrrz/CliWrap --depth 10");

// Build arguments from parts
var command = Cli.Wrap("git")
    .WithArguments(a => a
        .Add("clone")
        .Add("https://github.com/Tyrrrz/CliWrap")
        .Add("--depth")
        .Add(10));
```

When using the second approach, CliWrap builds the string automatically, appending individual arguments and escaping any special characters that need to be escaped. In general, this approach is more preferable than the first one because you don't have to worry about formatting.

You can also configure other aspects, such as environment variables and working directory:

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

Note that each call to one of these `WithXyz()` methods returns a new immutable object, with the corresponding property set to the specified value. That means you can safely re-use parts of your commands as you see fit:

```csharp
var command1 = Cli.Wrap("git")
    .WithWorkingDirectory("path/to/repo/")
    .WithArguments("--version")
    .WithEnvironmentVariables(e => e
        .Set("GIT_AUTHOR_NAME", "John")
        .Set("GIT_AUTHOR_EMAIL", "john@email.com"));

// Doing this does not affect command1 in any way
var command2 = command1.WithArguments("pull");
```

Additionally, you can also use `WithValidation()` to configure whether the command will throw exception in case of a non-zero exit code:

```csharp
// This command will not throw an exception when it produces a non-zero exit code
var command = Cli.Wrap("git").WithValidation(CommandResultValidation.None);
```

By default, commands have this validation enabled.

### Cancelling execution

When executing a command, you can trigger a `CancellationToken` to signal it to abort. This will cause the underlying process to be killed and an exception to be thrown.

```csharp
using var cts = new CancellationTokenSource();

// Cancel automatically after a timeout of 10 seconds
cts.CancelAfter(TimeSpan.FromSeconds(10));

// If this is canceled, a TaskCanceledException is thrown
var result = await Cli.Wrap("path/to/exe").ExecuteAsync(cts.Token);
```

You can do the same with `ExecuteBufferedAsync()` and other execution models.

### Executing a command as an event stream

CliWrap also supports an alternative method of running commands, which is by representing the execution as an event stream. There are two models for this, pull-based and push-based.

#### Pull-based event streams

Pull-based event streams can be obtained by calling `command.ListenAsync()` which returns an `IAsyncEnumerable<CommandEvent>`. This is an asynchronous data stream that you can iterate through using the `await foreach` semantics introduced in C# 8.

`CommandEvent` itself is an abstract type which can be either one of the following:

- `StartedCommandEvent` -- received just once, when the command starts executing. Contains the process ID.
- `StandardOutputCommandEvent` -- received every time the underlying process writes a new line to output stream. Contains the text as string.
- `StandardErrorCommandEvent` -- received every time the underlying process writes a new line to error stream. Contains the text as string.
- `ExitedCommandEvent` -- received just once, when the command successfully finishes executing. Contains the exit code.

You can use a `switch` statement or a series of `if` statements to perform pattern matching and handle events of different types.

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

There also overloads for `ListenAsync()` that accept encoding options and provide cancellation support.

#### Push-based event streams

Push-based event streams can be obtained by calling `command.Observe()` which returns a cold `IObservable<CommandEvent>`.

The following example uses the [`System.Reactive`](https://github.com/dotnet/reactive) package, which is a set of extensions that make working with `IObservable<T>` much more convenient.

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

Using reactive extensions, you can do a lot of different things with event streams, including filtering, transforming, reducing, merging, etc.

There also overloads for `Observe()` that accept encoding options and provide cancellation support.

### Piping

Most of the features you've seen so far are based on CliWrap's core model of piping. It lets you redirect input and output streams of the underlying process to form a complex execution pipeline.

To facilitate piping, `Command` object has three methods: `WithStandardInputPipe(PipeSource source)`, `WithStandardOutputPipe(PipeTarget target)`, `WithStandardErrorPipe(PipeTarget target)`, which let you specify how you want to have the streams redirected. Similarly to other `WithXyz()` methods, these produce new immutable commands, so any existing command instances are unaffected.

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

There are many ways you can create new instances of `PipeSource` and `PipeTarget`:

- `PipeSource.Null` -- represents an empty pipe source.
- `PipeSource.FromStream()` -- pipes data from any readable stream.
- `PipeSource.FromBytes()` -- pipes data from a byte array.
- `PipeSource.FromString()` -- pipes from a text string (supports custom encoding)
- `PipeSource.FromCommand()` -- pipes data from standard output of another command.
- `PipeTarget.Null` - represents a pipe target that discards all data.
- `PipeTarget.ToStream()` -- pipes data into any writeable stream.
- `PipeTarget.ToStringBuilder()` -- pipes data as text into `StringBuilder` (supports custom encoding).
- `PipeTarget.ToDelegate()` -- pipes data as text, line-by-line, into `Action<string>` (supports custom encoding).
- `PipeTarget.Merge()` -- merges multiple pipes into one.

The pipe operator also has overloads for most of these. Here are some examples of what you can do:

```csharp
// Pipe a string as stdin
await ("Hello world" | Cli.Wrap("foo")).ExecuteAsync();

// Pipe stdout as text into a string builder
var stdOutBuffer = new StringBuilder();
await (Cli.Wrap("foo") | stdOutBuffer).ExecuteAsync();

// Pipe an HTTP stream as stdin
using var httpClient = new HttpClient();
await using var input = await httpClient.GetStreamAsync("https://example.com/image.png");

await (input | Cli.Wrap("foo")).ExecuteAsync();

// Pipe stdout of one command into another command's stdin
await (Cli.Wrap("foo") | Cli.Wrap("bar") | Cli.Wrap("baz")).ExecuteAsync();

// Pipe stdout and stderr into stdout and stderr of the parent process
await using var stdOut = Console.OpenStandardOutput();
await using var stdErr = Console.OpenStandardError();

await (Cli.Wrap("foo") | (stdOut, stdErr)).ExecuteAsync();

// ...or the same, though slightly less efficient due to re-encoding
await (Cli.Wrap("foo") | (Console.WriteLine, Console.Error.WriteLine)).ExecuteAsync();

// Pipe stdout into file and stderr into a buffer
await using var file = File.Create("output.txt");
var buffer = new StringBuilder();

await (Cli.Wrap("foo") | (PipeTarget.ToStream(file), PipeTarget.ToStringBuilder(buffer))).ExecuteAsync();

// Pipe into multiple files simultaneously
await using var file1 = File.Create("file1.txt");
await using var file2 = File.Create("file2.txt");
await using var file3 = File.Create("file3.txt");

var target = PipeTarget.Merge(
    PipeTarget.ToStream(file1)
    PipeTarget.ToStream(file2)
    PipeTarget.ToStream(file3));

await (Cli.Wrap("foo") | target).ExecuteAsync();

// Pipe a string into a command, into another command, into parent's stdout and stderr
var command = "Hello world" | Cli.Wrap("foo")
    .WithArguments("print random") | Cli.Wrap("bar")
    .WithArguments("reverse") | (Console.WriteLine, Console.Error.WriteLine);

var result = await command.ExecuteAsync();
```

Piping is not only used for convenience but also to avoid memory allocations that would otherwise be caused by buffering data in memory. CliWrap makes building command pipelines very simple -- just imagine doing the same thing with `System.Diagnostics.Process`.

As you can probably guess, the extension methods mentioned earlier, `ExecuteBufferedAsync()`, `ListenAsync()` and `Observe()`, are all based on this piping model. In fact we can even go a level deeper and combine these approaches together:

```csharp
await using var input = File.OpenRead("input.txt");
await using var output = File.Create("output.txt");

// Calling ListenAsync() preserves existing pipes by merging them with the ones it uses internally
await foreach (var cmdEvent in (input | Cli.Wrap("foo") | output).ListenAsync())
{
    // Process event stream
}
```
