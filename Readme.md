# CliWrap

[![Build](https://github.com/Tyrrrz/CliWrap/workflows/CI/badge.svg?branch=master)](https://github.com/Tyrrrz/CliWrap/actions)
[![Coverage](https://codecov.io/gh/Tyrrrz/CliWrap/branch/master/graph/badge.svg)](https://codecov.io/gh/Tyrrrz/CliWrap)
[![Version](https://img.shields.io/nuget/v/CliWrap.svg)](https://nuget.org/packages/CliWrap)
[![Downloads](https://img.shields.io/nuget/dt/CliWrap.svg)](https://nuget.org/packages/CliWrap)
[![Discord](https://img.shields.io/discord/869237470565392384?label=discord)](https://discord.gg/2SUWKFnHSm)
[![Donate](https://img.shields.io/badge/donate-$$$-purple.svg)](https://tyrrrz.me/donate)

✅ **Project status: active**.

CliWrap is a library for interacting with external command line interfaces.
It provides a convenient model for launching processes, redirecting input and output streams, awaiting completion, handling cancellation, and more.

💬 **If you want to chat, join my [Discord server](https://discord.gg/2SUWKFnHSm)**.

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

Similarly to a shell, CliWrap's base unit of work is a **command** -- an object that encodes instructions for running a process.
To build a command, start by calling `Cli.Wrap(...)` with the executable path, and then use the provided fluent interface to configure arguments, working directory, or other options.
Once the command is configured, you can run it by calling `ExecuteAsync()`:

```csharp
using CliWrap;

var result = await Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .WithWorkingDirectory("work/dir/path")
    .ExecuteAsync();
    
// Result contains:
// -- result.ExitCode        (int)
// -- result.StartTime       (DateTimeOffset)
// -- result.ExitTime        (DateTimeOffset)
// -- result.RunTime         (TimeSpan)
```

The code above spawns a child process with the configured command line arguments and working directory, and then asynchronously waits for it to exit.
After the task has completed, it resolves a `CommandResult` object that contains the process exit code and other related information.

> Note that CliWrap will throw an exception if the underlying process returns a non-zero exit code, as it usually indicates an error.
You can [override this behavior](#command-configuration) by disabling result validation using `WithValidation(CommandResultValidation.None)`.

By default, the process's standard input, output and error streams are routed to CliWrap's equivalent of the [_null device_](https://en.wikipedia.org/wiki/Null_device), which represents an empty source and a target that discards all data.
You can change this by calling `WithStandardInputPipe(...)`, `WithStandardOutputPipe(...)`, or `WithStandardErrorPipe(...)` to configure pipes for the corresponding streams:

```csharp
var stdOutBuffer = new StringBuilder();
var stdErrBuffer = new StringBuilder();

var result = await Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .WithWorkingDirectory("work/dir/path")
    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
    .ExecuteAsync();
    
// Contains stdOut/stdErr buffered in-memory as string
var stdOut = stdOutBuffer.ToString(); 
var stdErr = stdErrBuffer.ToString();
```

In this example, the data pushed to standard output and error streams is decoded as text and written to separate `StringBuilder` buffers.
After the command has finished executing, you can inspect the contents of these buffers to see what the process has printed to the console during its runtime.

Handling command output is a very common use case, so CliWrap offers a few high-level [execution models](#execution-models) to make these scenarios simpler.
In particular, the same thing shown above can also be achieved more succinctly with the `ExecuteBufferedAsync()` extension method:

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

> Note that standard streams are not limited to text and can contain raw binary data.
Additionally, the size of the data may make it inefficient to store in-memory.
For more complex scenarios, CliWrap also provides other piping options, which are covered in the [Piping](#piping) section.

### Command configuration

The fluent interface provided by the command object allows you to configure various options related to its execution.
Below list covers all available configuration methods and their usage.

> Note that `Command` is an immutable object, meaning that all configuration methods listed here return a new instance instead of modifying the existing one.

#### `WithArguments(...)`

Sets the command line arguments that will be passed to the child process.

Default: empty.

Set arguments directly from a string:

```csharp
var cmd = Cli.Wrap("git")
    .WithArguments("commit -m \"my commit\"");
```

Set arguments from a list (each element is treated as a separate argument; spaces are escaped automatically):

```csharp
var cmd = Cli.Wrap("git")
    .WithArguments(new[] {"commit", "-m", "my commit"});
```

Set arguments using a builder (same as above, but also automatically converts certain values to their string representations):

```csharp
var cmd = Cli.Wrap("git")
    .WithArguments(args => args
        .Add("clone")
        .Add("https://github.com/Tyrrrz/CliWrap")
        .Add("--depth")
        .Add(20)); // <- formatted to a string
```

#### `WithWorkingDirectory(...)`

Sets the working directory of the child process.

Default: current working directory, i.e. `Directory.GetCurrentDirectory()`.

```csharp
var cmd = Cli.Wrap("git")
    .WithWorkingDirectory("c:/projects/my project/");
```

#### `WithEnvironmentVariables(...)`

Sets additional environment variables that will be exposed to the child process.

Default: empty.

Set environment variables from a dictionary:

```csharp
var cmd = Cli.Wrap("git")
    .WithEnvironmentVariables(new Dictionary<string, string?>
    {
        ["GIT_AUTHOR_NAME"] = "John",
        ["GIT_AUTHOR_EMAIL"] = "john@email.com"
    });
```

Set environment variables using a builder:

```csharp
var cmd = Cli.Wrap("git")
    .WithEnvironmentVariables(env => env
        .Set("GIT_AUTHOR_NAME", "John")
        .Set("GIT_AUTHOR_EMAIL", "john@email.com"));
```

> Note that these environment variables are set on top of the default environment variables inherited from the parent process.
If you provide a variable with the same name as one of the inherited variables, the provided value will take precedence.
Additionally, you can also remove an inherited variable by setting its value to `null`.

#### `WithCredentials(...)`

Sets domain, name and password of the user, under whom the child process will be started.

Default: no credentials.

Set credentials directly:

```csharp
var cmd = Cli.Wrap("git")
    .WithCredentials(new Credentials(
        "some_workspace",
        "johndoe",
        "securepassword123"
    ));
```

Set credentials using a builder:

```csharp
var cmd = Cli.Wrap("git")
    .WithCredentials(creds => creds
       .SetDomain("some_workspace")
       .SetUserName("johndoe")
       .SetPassword("securepassword123"));
```

> Note that specifying domain and password is only supported on Windows and will result in an exception on other operating systems.
Specifying username, on the other hand, is supported across all platforms.

#### `WithValidation(...)`

Sets the strategy for validating the result of an execution.

The following modes are available:

- `CommandResultValidation.None` -- no validation
- `CommandResultValidation.ZeroExitCode` -- ensures zero exit code when the process exits

Default: `CommandResultValidation.ZeroExitCode`.

Disable validation:

```csharp
var cmd = Cli.Wrap("git")
    .WithValidation(CommandResultValidation.None);
```

Enable validation:

```csharp
// Ensure that exit code is zero after the process exits (otherwise throw an exception)
var cmd = Cli.Wrap("git")
    .WithValidation(CommandResultValidation.ZeroExitCode);
```

#### `WithStandardInputPipe(...)`

Sets the pipe source that will be used for the standard _input_ stream of the process.

Default: `PipeSource.Null`.

_Read more about this method in the [Piping](#piping) section._

#### `WithStandardOutputPipe(...)`

Sets the pipe target that will be used for the standard _output_ stream of the process.

Default: `PipeTarget.Null`.

_Read more about this method in the [Piping](#piping) section._

#### `WithStandardErrorPipe(...)`

Sets the pipe target that will be used for the standard _error_ stream of the process.

Default: `PipeTarget.Null`.

_Read more about this method in the [Piping](#piping) section._

### Piping

CliWrap provides a very powerful and flexible piping model that allows you to redirect process's streams, transform input and output data, and even chain multiple commands together with minimal effort.
At its core, it's based on two abstractions: `PipeSource` which provides data for standard input stream, and `PipeTarget` which reads data coming from standard output or standard error streams.

By default, command's input pipe is set to `PipeSource.Null` and the output and error pipes are set to `PipeTarget.Null`.
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

Alternatively, pipes can also be configured in a slightly terser way by using pipe operators:

```csharp
await using var input = File.OpenRead("input.txt");
await using var output = File.Create("output.txt");

await (input | Cli.Wrap("foo") | output).ExecuteAsync();
```

Both `PipeSource` and `PipeTarget` have many factory methods that let you create pipe implementations for different scenarios:

- `PipeSource`:
  - `PipeSource.Null` -- represents an empty pipe source
  - `PipeSource.FromStream(...)` -- pipes data from any readable stream
  - `PipeSource.FromFile(...)` -- pipes data from a file
  - `PipeSource.FromBytes(...)` -- pipes data from a byte array
  - `PipeSource.FromString(...)` -- pipes from a text string
  - `PipeSource.FromCommand(...)` -- pipes data from standard output of another command
- `PipeTarget`:
  - `PipeTarget.Null` -- represents a pipe target that discards all data
  - `PipeTarget.ToStream(...)` -- pipes data into any writeable stream
  - `PipeTarget.ToFile(...)` -- pipes data into a file
  - `PipeTarget.ToStringBuilder(...)` -- pipes data as text into `StringBuilder`
  - `PipeTarget.ToDelegate(...)` -- pipes data as text, line-by-line, into `Action<string>` or `Func<string, Task>`
  - `PipeTarget.Merge(...)` -- merges multiple outbound pipes by replicating the same data across all of them

Below you can see some examples of what you can achieve with the help of CliWrap's piping feature.

#### Pipe a string into stdin

```csharp
var cmd = "Hello world" | Cli.Wrap("foo");
await cmd.ExecuteAsync();
```

#### Pipe stdout as text into a `StringBuilder`

```csharp
var stdOutBuffer = new StringBuilder();

var cmd = Cli.Wrap("foo") | stdOutBuffer;
await cmd.ExecuteAsync();
```

#### Pipe a binary HTTP stream into stdin

```csharp
using var httpClient = new HttpClient();
await using var input = await httpClient.GetStreamAsync("https://example.com/image.png");

var cmd = input | Cli.Wrap("foo");
await cmd.ExecuteAsync();
```

#### Pipe stdout of one command into stdin of another

```csharp
var cmd = Cli.Wrap("foo") | Cli.Wrap("bar") | Cli.Wrap("baz");
await cmd.ExecuteAsync();
```

#### Pipe stdout and stderr into parent process

```csharp
await using var stdOut = Console.OpenStandardOutput();
await using var stdErr = Console.OpenStandardError();

var cmd = Cli.Wrap("foo") | (stdOut, stdErr);
await cmd.ExecuteAsync();
```

#### Pipe stdout to a delegate

```csharp
var cmd = Cli.Wrap("foo") | Debug.WriteLine;
await cmd.ExecuteAsync();
```

#### Pipe stdout into a file and stderr into a `StringBuilder`

```csharp
var buffer = new StringBuilder();

var cmd = Cli.Wrap("foo") |
    (PipeTarget.ToFile("output.txt"), PipeTarget.ToStringBuilder(buffer));

await cmd.ExecuteAsync();
```

#### Pipe stdout into multiple files simultaneously

```csharp
var target = PipeTarget.Merge(
    PipeTarget.ToFile("file1.txt"),
    PipeTarget.ToFile("file2.txt"),
    PipeTarget.ToFile("file3.txt")
);

var cmd = Cli.Wrap("foo") | target;
await cmd.ExecuteAsync();
```

#### Pipe a chain of commands

```csharp
var cmd = "Hello world" | Cli.Wrap("foo")
    .WithArguments("print random") | Cli.Wrap("bar")
    .WithArguments("reverse") | (Console.WriteLine, Console.Error.WriteLine);

await cmd.ExecuteAsync();
```

### Execution models

CliWrap provides a few high-level execution models, which are essentially just extension methods that offer alternative ways to reason about command execution.
Under the hood, they are all built by leveraging the [piping feature](#piping) shown earlier.

#### Buffered execution

This execution model lets you run a process while buffering its standard output and error streams in-memory.
The buffered data can then be accessed after the command finishes executing.

In order to execute a command with buffering, call the `ExecuteBufferedAsync()` extension method:

```csharp
using CliWrap;
using CliWrap.Buffered;

var result = await Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .ExecuteBufferedAsync();

var exitCode = result.ExitCode;    
var stdOut = result.StandardOutput;
var stdErr = result.StandardError;
```

By default, `ExecuteBufferedAsync()` assumes that the underlying process uses default encoding (`Console.OutputEncoding`) for writing text to the console.
To override this, specify the encoding explicitly by using one of the available overloads:

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
This lets you start a command and react to the events it produces in real-time.

Those events are:

- `StartedCommandEvent` -- received just once, when the command starts executing (contains process ID)
- `StandardOutputCommandEvent` -- received every time the underlying process writes a new line to the output stream (contains the text as string)
- `StandardErrorCommandEvent` -- received every time the underlying process writes a new line to the error stream (contains the text as string)
- `ExitedCommandEvent` -- received just once, when the command finishes executing (contains exit code)

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

The `ListenAsync()` method starts the command and returns an object of type `IAsyncEnumerable<CommandEvent>`, which you can iterate using the `await foreach` construct introduced in C# 8.
When using this execution model, back pressure is facilitated by locking the pipes between each iteration of the loop, preventing unnecessary buffering of data in-memory.

If you also need to specify custom encoding, you can use one of the available overloads:

```csharp
await foreach (var cmdEvent in cmd.ListenAsync(Encoding.UTF8))
{
    // ...
}

await foreach (var cmdEvent in cmd.ListenAsync(Encoding.ASCII, Encoding.UTF8))
{
    // ...
}
```

#### Push-based event stream

Similarly to the pull-based stream, you can also execute a command as a _push-based_ event stream instead:

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

In this case, `Observe()` returns a cold `IObservable<CommandEvent>` that represents an observable stream of command events.
You can use the set of extensions provided by [Rx.NET](https://github.com/dotnet/reactive) to transform, filter, throttle, or otherwise manipulate this stream.

Unlike with the pull-based event stream, this execution model does not involve any back pressure, meaning that the data is pushed to the observer at the rate it becomes available.

Likewise, if you also need to specify custom encoding, you can use one of the available overloads:

```csharp
var cmdEvents = cmd.Observe(Encoding.UTF8);

// ...

var cmdEvents = cmd.Observe(Encoding.ASCII, Encoding.UTF8);

// ...
```

#### Combining execution models with custom pipes

The different execution models shown above are based on the piping model, but those two concepts are not mutually exclusive.
That's because internally they all rely on `PipeTarget.Merge()`, which allows them to wire new pipes while still preserving those configured earlier.

This means that, for example, you can create a piped command and also execute it as an event stream:

```csharp
var cmd =
    PipeSource.FromFile("input.txt") |
    Cli.Wrap("foo") |
    PipeSource.ToFile("output.txt");

// Iterate as an event stream and pipe to file at the same time
// (pipes are combined, not overriden)
await foreach (var cmdEvent in cmd.ListenAsync())
{
    // ...
}
```

### Timeout and cancellation

Command execution is asynchronous in nature as it involves a completely separate process.
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

Similarly to `ExecuteAsync()`, cancellation is also supported by `ExecuteBufferedAsync()`, `ListenAsync()`, and `Observe()`:

```csharp
// Cancellation with buffered execution
var result = await Cli.Wrap("path/to/exe").ExecuteBufferedAsync(cts.Token);

// Cancellation with pull-based event stream
await foreach (Cli.Wrap("path/to/exe").ListenAsync(cts.Token))
{
    // ...
}

// Cancellation with push-based event stream
var cmdEvents = Cli.Wrap("path/to/exe").Observe(cts.Token);
```

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
