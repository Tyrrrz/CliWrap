# CliWrap

[![Build](https://github.com/Tyrrrz/CliWrap/workflows/CI/badge.svg?branch=master)](https://github.com/Tyrrrz/CliWrap/actions)
[![Coverage](https://codecov.io/gh/Tyrrrz/CliWrap/branch/master/graph/badge.svg)](https://codecov.io/gh/Tyrrrz/CliWrap)
[![Version](https://img.shields.io/nuget/v/CliWrap.svg)](https://nuget.org/packages/CliWrap)
[![Downloads](https://img.shields.io/nuget/dt/CliWrap.svg)](https://nuget.org/packages/CliWrap)
[![Donate](https://img.shields.io/badge/donate-$$$-purple.svg)](https://tyrrrz.me/donate)

✅ **Project status: active**.

CliWrap is a library for interacting with external command line interfaces.
It provides a convenient model for launching processes, redirecting input and output streams, awaiting completion, handling cancellation, and more.

## Download

📦 [NuGet](https://nuget.org/packages/CliWrap): `dotnet add package CliWrap`

## Features

- Airtight abstraction over `System.Diagnostics.Process`
- Fluent configuration interface
- Flexible support for piping
- Fully asynchronous and cancellation-aware API
- Designed with strict immutability in mind
- Provides safety against typical deadlock scenarios
- Tested on Windows, Linux, and macOS
- Targets .NET Standard 2.0+, .NET Core 3.0+, .NET Framework 4.6.1+
- No external dependencies

## Usage

### Quick overview

Similar to a shell, CliWrap's base unit of execution is the **command** -- an object that encodes instructions for running a process.
To build a command, start by calling `Cli.Wrap(...)` with the executable path, and then use the provided fluent interface to configure arguments, working directory, and other options:

```csharp
using CliWrap;

// Create a command
var cmd = Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .WithWorkingDirectory("work/dir/path");
```

Once the command has been created, you can run it by using the `ExecuteAsync()` method:

```csharp
// Execute the command
var result = await cmd.ExecuteAsync();

// Result contains:
// -- result.ExitCode        (int)
// -- result.StartTime       (DateTimeOffset)
// -- result.ExitTime        (DateTimeOffset)
// -- result.RunTime         (TimeSpan)
```

The code above spawns a child process with the configured command line arguments and working directory, and then asynchronously waits for it to exit.
After the task has completed, it resolves a `CommandResult` object that contains the process exit code and other related information.

> Note that `ExecuteAsync()` will throw an exception if the underlying process returns a non-zero exit code, as it usually indicates an error.
You can [override this behavior](#command-configuration) by disabling result validation using `WithValidation(CommandResultValidation.None)`.

By default, standard input, output and error streams are routed to CliWrap's equivalent of [_null device_](https://en.wikipedia.org/wiki/Null_device), which represents an empty source and a target that discards all data.
This can be changed by calling `WithStandardInputPipe(...)`, `WithStandardOutputPipe(...)`, or `WithStandardErrorPipe(...)` to configure pipes for the corresponding streams.

For example, here's the same command from earlier, configured to redirect its output and error streams into separate `StringBuilder` instances:

```csharp
var stdOutBuffer = new StringBuilder();
var stdErrBuffer = new StringBuilder();

var result = await Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .WithWorkingDirectory("work/dir/path")
    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stderrBuffer))
    .ExecuteAsync();
    
// Contains stdOut/stdErr buffered in-memory as string
var stdOut = stdOutBuffer.ToString(); 
var stdErr = stdErrBuffer.ToString();
```

In this case, instead of being ignored, the data written to standard output and error streams is decoded as text and stored in-memory.
You can inspect the contents of the buffers to see what the process has printed to the console during its execution. 

Handling command output is a very common scenario, so CliWrap provides a few higher level [execution models](#execution-models) to make things simpler.
In particular, the same thing shown in the example above can be achieved more succinctly by using the `ExecuteBufferedAsync()` extension method:

```csharp
using CliWrap;
using CliWrap.Buffered;

// Calling `ExecuteBufferedAsync()` instead of `ExecuteAsync()`
// implicitly configures pipes that write to in-memory buffers.
var result = await Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .WithWorkingDirectory("work/dir/path")
    .ExecuteBufferedAsync();

// Result contains:
// -- result.StandardOutput  (string)
// -- result.StandardError   (string)
// -- result.ExitCode        (int)
// -- result.StartTime       (DateTimeOffset)
// -- result.ExitTime        (DateTimeOffset)
// -- result.RunTime         (TimeSpan)
```

Calling `ExecuteBufferedAsync()` starts the process and implicitly wires its output and error streams to in-memory buffers.
Similarly to `ExecuteAsync()`, this method returns a result object containing runtime information, with the addition of `StandardOutput` and `StandardError` properties.

> Note that standard streams are not limited to text and can contain raw binary data.
Additionally, the size of the data may make it inefficient to store in-memory.
For more complex scenarios, CliWrap also provides other piping options, which are covered in the [Piping](#piping) section.

> Note that `ExecuteBufferredAsync()` is just one of high level execution models available in CliWrap.
Read the [Execution models](#execution-models) section to learn more.

### Command configuration

The fluent interface provided by the command object allows you to configure various options related to its execution.
Below you can find all of them:

- #### `WithArguments(...)`

Sets the command line arguments that will be passed to the child process.

Default: empty.

Example:

```csharp
// Set arguments directly
var cmd = Cli.Wrap("git")
    .WithArguments("commit -m \"my commit\"");

// Set arguments from a list
// (each element is a separate argument; spaces are escaped)
var cmd = Cli.Wrap("git")
    .WithArguments(new[] {"commit", "-m", "my commit"});

// Build arguments from parts
// (same as above, but also automatically formats non-string values)
var cmd = Cli.Wrap("git")
    .WithArguments(args => args
        .Add("clone")
        .Add("https://github.com/Tyrrrz/CliWrap")
        .Add("--depth")
        .Add(20)); // <- formatted to a string
```

ℹ️ It's recommended to use the last two overloads wherever possible as they take care of escaping special characters for you automatically.

- #### `WithWorkingDirectory(...)`

Sets the working directory of the child process.

Default: current working directory, i.e. `Directory.GetCurrentDirectory()`.

Example:

```csharp
var cmd = Cli.Wrap("git")
    .WithWorkingDirectory("c:/projects/my project/");
```

- #### `WithEnvironmentVariables(...)`

Sets additional environment variables that will be exposed to the child process.

Default: empty.

Example:

```csharp
// Set from a dictioanry
var cmd = Cli.Wrap("git")
    .WithEnvironmentVariables(new Dictionary<string, string>
    {
        ["GIT_AUTHOR_NAME"] = "John",
        ["GIT_AUTHOR_EMAIL"] = "john@email.com"
    });
    
// Set using a builder
var cmd = Cli.Wrap("git")
    .WithEnvironmentVariables(env => env
        .Set("GIT_AUTHOR_NAME", "John")
        .Set("GIT_AUTHOR_EMAIL", "john@email.com"));
```

> Note that these environment variables are set on top of the default environment variables inherited from the parent process.
If you provide a variable with the same name as one of the inherited variables, the provided value will take precedence.

- #### `WithCredentials(...)`

Sets domain, name and password of the user, under whom the child process will be started.

Default: no credentials.

Example:

```csharp
// Set directly
var cmd = Cli.Wrap("git")
    .WithCredentials(new Credentials(
        "some_workspace",
        "johndoe",
        "securepassword123"
    ));

// Set using a builder
var cmd = Cli.Wrap("git")
    .WithCredentials(creds => creds
       .SetDomain("some_workspace")
       .SetUserName("johndoe")
       .SetPassword("securepassword123"));
```

> Note that specifying domain and password is only supported on Windows and will result in an exception on other operating systems.
Username, on the other hand, is supported across all platforms.

- #### `WithValidation(...)`

Sets the strategy for validating the result of an execution.

The following modes are available:

- `CommandResultValidation.None` -- no validation
- `CommandResultValidation.ZeroExitCode` -- ensures zero exit code when the process exits

Default: `CommandResultValidation.ZeroExitCode`.

Example:

```csharp
// No validation
var cmd = Cli.Wrap("git")
    .WithValidation(CommandResultValidation.None);
    
// Ensure that exit code is zero after the process exits (otherwise throw an exception)
var cmd = Cli.Wrap("git")
    .WithValidation(CommandResultValidation.ZeroExitCode);
```

- #### `WithStandardInputPipe(...)`

Sets the pipe source that will be used for the standard _input_ stream of the process.

Default: `PipeSource.Null`.

Read more about piping in the [next section](#piping).

- #### `WithStandardOutputPipe(...)`

Sets the pipe target that will be used for the standard _output_ stream of the process.

Default: `PipeTarget.Null`.

Read more about piping in the [next section](#piping).

- #### `WithStandardErrorPipe(...)`

Sets the pipe target that will be used for the standard _error_ stream of the process.

Default: `PipeTarget.Null`.

Read more about piping in the [next section](#piping).

> Note that `Command` is an immutable object, so all configuration methods (i.e. those prefixed by `With...`) return a new instance instead of modifying an existing one.
This means that you can reuse commands safely without worrying about potential side-effects.

### Piping

CliWrap offers a very powerful and flexible piping model that allows you to redirect process's streams, transform their data, or even chain them together with minimal effort.
At its core, it's based on two abstractions: `PipeSource` which provides data for a standard input stream, and `PipeTarget` which reads and processes data coming from a standard output or error stream.

By default, command's input pipe is configured to `PipeSource.Null` and the output and error pipes are configured to `PipeTarget.Null`.
These objects effectively represent no-op stubs that provide empty input and discard all output respectively.

You can specify your own `PipeSource` and `PipeTarget` instances by calling the corresponding configuration methods on the command:

```csharp
await using var input = File.OpenRead("input.txt");
await using var output = File.Create("output.txt");

await Cli.Wrap("foo")
    .WithStandardInputPipe(PipeSource.FromStream(input))
    .WithStandardOutputPipe(PipeTarget.ToStream(output))
    .ExecuteAsync();
```

The exact same thing shown above can also be expressed in a terser way using pipe operators:

```csharp
await using var input = File.OpenRead("input.txt");
await using var output = File.Create("output.txt");

await (input | Cli.Wrap("foo") | output).ExecuteAsync();
```

Besides raw streams, `PipeSource` and `PipeTarget` both have factory methods that let you create different pipe implementations:

- `PipeSource.Null` -- represents an empty pipe source.
- `PipeSource.FromStream()` -- pipes data from any readable stream.
- `PipeSource.FromFile()` -- pipes data from a file identified by its path.
- `PipeSource.FromBytes()` -- pipes data from a byte array.
- `PipeSource.FromString()` -- pipes from a text string (supports custom encoding).
- `PipeSource.FromCommand()` -- pipes data from standard output of another command.
- `PipeTarget.Null` -- represents a pipe target that discards all data.
- `PipeTarget.ToStream()` -- pipes data into any writeable stream.
- `PipeTarget.ToFile()` -- pipes data into a file identified by its path.
- `PipeTarget.ToStringBuilder()` -- pipes data as text into `StringBuilder` (supports custom encoding).
- `PipeTarget.ToDelegate()` -- pipes data as text, line-by-line, into `Action<string>` or `Func<string, Task>` (supports custom encoding).
- `PipeTarget.Merge()` -- merges multiple outbound pipes into one.

The pipe operator also has overloads for most of these.
Below you can see some examples of what you can achieve with the piping feature that CliWrap provides.

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
var buffer = new StringBuilder();

var cmd = Cli.Wrap("foo") |
    (PipeTarget.ToFile("output.txt"), PipeTarget.ToStringBuilder(buffer));

await cmd.ExecuteAsync();
```

Pipe stdout into multiple files simultaneously:

```csharp
var target = PipeTarget.Merge(
    PipeTarget.ToFile("file1.txt"),
    PipeTarget.ToFile("file2.txt"),
    PipeTarget.ToFile("file3.txt")
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

As you can see, piping enables a wide range of different use cases.
It's not only used for convenience, but also to improve memory efficiency when dealing with large and/or binary data.

### Execution models

CliWrap provides a few high level execution models, which are essentially extension methods that allow you to reason about command execution in different ways.
Their primary purpose is to simplify configuration for common use cases.

#### Buffered execution

This execution model lets you run a process while buffering its standard output and error streams in-memory.
The data is processed as text and can subsequently be accessed once the command finishes executing.

```csharp
using CliWrap;
using CliWrap.Buffered;

var result = await Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .ExecuteBufferedAsync();
    
var stdOut = result.StandardOutput;
var stdErr = result.StandardError;
```

By default, `ExecuteBufferedAsync()` will assume that the underlying process uses default encoding (`Console.OutputEncoding`) for writing text to the console.
You can also specify the encoding explicitly by using one of the available overloads:

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

#### Pull-based event stream

Besides executing a command as a task, CliWrap also supports an alternative model, in which the execution is represented as an event stream.
This lets you start a command and react to the events it produces in real time.

Those events are:

- `StartedCommandEvent` -- received just once, when the command starts executing. Contains the process ID.
- `StandardOutputCommandEvent` -- received every time the underlying process writes a new line to the output stream. Contains the text as string.
- `StandardErrorCommandEvent` -- received every time the underlying process writes a new line to the error stream. Contains the text as string.
- `ExitedCommandEvent` -- received just once, when the command finishes executing. Contains the exit code.

To execute a command as a _pull-based_ event stream, use the `ListenAsync()` extension method:

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

The `ListenAsync()` method starts the command and returns an object of type `IAsyncEnumerable<CommandEvent>`, which you can iterate over using the `await foreach` construct introduced in C# 8.
In this scenario, back pressure is performed by locking the pipes between each iteration of the loop, which means that the underlying process may suspend execution until the event is fully processed.

#### Push-based event stream

Similarly to the pull-based stream, you can also execute a command as an observable _push-based_ event stream instead:

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

In this case, `Observe()` returns a cold `IObservable<CommandEvent>` that represents an observable stream of events.
You can use the set of extensions provided by [Rx.NET](https://github.com/dotnet/reactive) to transform, filter, throttle, or otherwise manipulate the stream.

Unlike with the pull-based approach, this scenario does not involve any back pressure so the data is pushed to the observers at the rate it becomes available.

#### Combining execution models with custom pipes

The different execution models, `ExecuteBufferedAsync()`, `ListenAsync()` and `Observe()` are all based on the piping model, but those concepts are not mutually exclusive.
For example, you can create a piped command and then start it as an event stream:

```csharp
var cmd =
    PipeSource.FromFile("input.txt") |
    Cli.Wrap("foo") |
    PipeSource.ToFile("output.txt");

await foreach (var cmdEvent in cmd.ListenAsync())
{
    // ...
}
```

In this scenario, the pipes configured previously are not overriden when calling `ListenAsync()`.
This is facilitated by relying `PipeTarget.Merge()` internally to combine new pipes with those configured earlier.

### Timeout and cancellation

Command execution is asynchronous by nature as it involves a completely separate process.
In many cases, it may be useful to implement an abortion mechanism to stop the execution before it finishes, either through a manual trigger or a timeout.

To do that, just pass the corresponding `CancellationToken` when calling `ExecuteAsync()`:

```csharp
using var cts = new CancellationTokenSource();

// Cancel automatically after a timeout of 10 seconds
cts.CancelAfter(TimeSpan.FromSeconds(10));

var result = await Cli.Wrap("path/to/exe").ExecuteAsync(cts.Token);
```

In the event that the cancellation is requested, the underlying process will get killed and the `ExecuteAsync()` will throw an exception of type `OperationCanceledException` (or its derivative, `TaskCanceledException`).
You will need to catch this exception in your code to recover from cancellation.

> Similarly to `ExecuteAsync()`, cancellation is also supported by `ExecuteBufferedAsync()`, `ListenAsync()`, and `Observe()`.

### Retrieving process ID

The task returned by `ExecuteAsync()` and `ExecuteBufferedAsync()` is in fact not a regular `Task<T>`, but an instance of `CommandTask<T>`.
This is a special awaitable object that contains additional information related to a command which is currently executing.

You can inspect the task while it's running to get the ID of the process that was started by the associated command:

```csharp
var task = Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .ExecuteAsync();

// Get the process ID (for example, for logging purposes)
var processId = task.ProcessId;

// Wait for the task to complete
await task;
```
