# CliWrap

[![Made in Ukraine](https://img.shields.io/badge/made_in-ukraine-ffd700.svg?labelColor=0057b7)](https://vshymanskyy.github.io/StandWithUkraine)
[![Build](https://img.shields.io/github/actions/workflow/status/Tyrrrz/CliWrap/main.yml?branch=master)](https://github.com/Tyrrrz/CliWrap/actions)
[![Coverage](https://img.shields.io/codecov/c/github/Tyrrrz/CliWrap/master)](https://codecov.io/gh/Tyrrrz/CliWrap)
[![Version](https://img.shields.io/nuget/v/CliWrap.svg)](https://nuget.org/packages/CliWrap)
[![Downloads](https://img.shields.io/nuget/dt/CliWrap.svg)](https://nuget.org/packages/CliWrap)
[![Discord](https://img.shields.io/discord/869237470565392384?label=discord)](https://discord.gg/2SUWKFnHSm)
[![Donate](https://img.shields.io/badge/donate-$$$-8a2be2.svg)](https://tyrrrz.me/donate)
[![Fuck Russia](https://img.shields.io/badge/fuck-russia-e4181c.svg?labelColor=000000)](https://twitter.com/tyrrrz/status/1495972128977571848)

> 🟢 **Project status**: active<sup>[[?]](https://github.com/Tyrrrz/.github/blob/master/docs/project-status.md)</sup>

**CliWrap** is a library for interacting with external command line interfaces.
It provides a convenient model for launching processes, redirecting input and output streams, awaiting completion, handling cancellation, and more.

## Terms of use<sup>[[?]](https://github.com/Tyrrrz/.github/blob/master/docs/why-so-political.md)</sup>

By using this project or its source code, for any purpose and in any shape or form, you grant your **implicit agreement** to all the following statements:

- You **condemn Russia and its military aggression against Ukraine**
- You **recognize that Russia is an occupant that unlawfully invaded a sovereign state**
- You **support Ukraine's territorial integrity, including its claims over temporarily occupied territories of Crimea and Donbas**
- You **reject false narratives perpetuated by Russian state propaganda**

To learn more about the war and how you can help, [click here](https://tyrrrz.me). Glory to Ukraine! 🇺🇦

## Install

- 📦 [NuGet](https://nuget.org/packages/CliWrap): `dotnet add package CliWrap`

## Features

- Airtight abstraction over `System.Diagnostics.Process`
- Fluent configuration interface
- Flexible support for piping
- Fully asynchronous and cancellation-aware API
- Graceful cancellation using interrupt signals
- Designed with strict immutability in mind
- Provides safety against typical deadlock scenarios
- Tested on Windows, Linux, and macOS
- Targets .NET Standard 2.0+, .NET Core 3.0+, .NET Framework 4.6.2+
- No external dependencies

## Usage

### Video guides

You can watch one of these videos to learn more about the library:

- [**OSS Power-Ups: CliWrap**](https://youtube.com/watch?v=3_Ucw3Fflmo) by [Oleksii Holub](https://twitter.com/tyrrrz)

[![Intro to CliWrap](.assets/video-guide-oss-powerups.jpg)](https://youtube.com/watch?v=3_Ucw3Fflmo)

- [**Stop using the Process class for CLI interactions in .NET**](https://youtube.com/watch?v=Pt-0KM5SxmI) by [Nick Chapsas](https://twitter.com/nickchapsas)

[![Stop using the Process class for CLI interactions in .NET](.assets/video-guide-nick-chapsas.jpg)](https://youtube.com/watch?v=Pt-0KM5SxmI)

### Quick overview

Similarly to a shell, **CliWrap**'s base unit of work is a **command** — an object that encapsulates instructions for running a process.
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
After the task has completed, it resolves to a `CommandResult` object that contains the process exit code and other related information.

> **Warning**:
> **CliWrap** will throw an exception if the underlying process returns a non-zero exit code, as it usually indicates an error.
> You can [override this behavior](#withvalidation) by disabling result validation using `WithValidation(CommandResultValidation.None)`.

By default, the process's standard input, output and error streams are routed to **CliWrap**'s equivalent of a [_null device_](https://en.wikipedia.org/wiki/Null_device), which represents an empty source and a target that discards all data.
You can change this by calling `WithStandardInputPipe(...)`, `WithStandardOutputPipe(...)`, or `WithStandardErrorPipe(...)` to configure pipes for the corresponding streams:

```csharp
using CliWrap;

var stdOutBuffer = new StringBuilder();
var stdErrBuffer = new StringBuilder();

// ⚠ This particular example can also be simplified with ExecuteBufferedAsync().
// Continue reading below!
var result = await Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .WithWorkingDirectory("work/dir/path")
    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
    .ExecuteAsync();

// Access stdout & stderr buffered in-memory as strings
var stdOut = stdOutBuffer.ToString();
var stdErr = stdErrBuffer.ToString();
```

This example command is configured to decode the data written to standard output and error streams as text, and append it to the corresponding `StringBuilder` buffers.
After the execution is complete, these buffers can be inspected to see what the process has printed to the console.

Handling command output is a very common use case, so **CliWrap** offers a few high-level [execution models](#execution-models) to make these scenarios simpler.
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

> **Warning**:
> Be mindful when using `ExecuteBufferedAsync()`.
> Programs can write arbitrary data (including binary) to output and error streams, which may be impractical to store in-memory.
> For more advanced scenarios, **CliWrap** also provides other piping options, which are covered in the [piping section](#piping).

### Command configuration

The fluent interface provided by the command object allows you to configure various aspects of its execution.
This section covers all available configuration methods and their usage.

> **Note**:
> `Command` is an immutable object — all configuration methods listed here create a new instance instead of modifying the existing one.

#### `WithArguments(...)`

Sets the command line arguments passed to the child process.

**Default**: empty.

**Examples**:

- Set arguments from a string:

```csharp
var cmd = Cli.Wrap("git")
    .WithArguments("commit -m \"my commit\"");
```

> **Warning**:
> Unless you absolutely have to, avoid setting command arguments from a string.
> This method expects that all of the arguments are already escaped and formatted properly — which can be really hard to get right.

- Set arguments from an array — each element is treated as a separate argument and special characters are escaped automatically:

```csharp
var cmd = Cli.Wrap("git")
    .WithArguments(new[] {"commit", "-m", "my commit"});
```

- Set arguments using a builder — same as above, but also works with non-string arguments and can be [enhanced with your own extension methods](https://twitter.com/Tyrrrz/status/1409104223753605121):

```csharp
var cmd = Cli.Wrap("git")
    .WithArguments(args => args
        .Add("clone")
        .Add("https://github.com/Tyrrrz/CliWrap")
        .Add("--depth")
        .Add(20)
    );
```

> **Note**:
> You can also manually instantiate `ArgumentsBuilder` to help with the formatting and escaping of arguments.
> This may be useful if you need to generate an argument string outside of the `WithArguments(...)` method.

#### `WithWorkingDirectory(...)`

Sets the working directory of the child process.

**Default**: current working directory, i.e. `Directory.GetCurrentDirectory()`.

**Example**:

```csharp
var cmd = Cli.Wrap("git")
    .WithWorkingDirectory("c:/projects/my project/");
```

#### `WithEnvironmentVariables(...)`

Sets additional environment variables exposed to the child process.

**Default**: empty.

**Examples**:

- Set environment variables from a dictionary:

```csharp
var cmd = Cli.Wrap("git")
    .WithEnvironmentVariables(new Dictionary<string, string?>
    {
        ["GIT_AUTHOR_NAME"] = "John",
        ["GIT_AUTHOR_EMAIL"] = "john@email.com"
    });
```

- Set environment variables using a builder:

```csharp
var cmd = Cli.Wrap("git")
    .WithEnvironmentVariables(env => env
        .Set("GIT_AUTHOR_NAME", "John")
        .Set("GIT_AUTHOR_EMAIL", "john@email.com")
    );
```

> **Note**:
> Environment variables configured using `WithEnvironmentVariables(...)` are applied on top of those inherited from the parent process.
> If you need to remove an inherited variable, set the corresponding value to `null`.

#### `WithCredentials(...)`

Sets domain, name and password of the user, under whom the child process is started.

**Default**: no credentials.

**Examples**:

- Set credentials directly:

```csharp
var cmd = Cli.Wrap("git")
    .WithCredentials(new Credentials(
        domain: "some_workspace",
        userName: "johndoe",
        password: "securepassword123",
        loadUserProfile: true
    ));
```

- Set credentials using a builder:

```csharp
var cmd = Cli.Wrap("git")
    .WithCredentials(creds => creds
       .SetDomain("some_workspace")
       .SetUserName("johndoe")
       .SetPassword("securepassword123")
       .LoadUserProfile()
    );
```

> **Warning**:
> Running a process under a different username is supported across all platforms, but other options are only available on Windows.

#### `WithValidation(...)`

Sets the strategy for validating the result of an execution.

**Accepted values**:

- `CommandResultValidation.None` — no validation
- `CommandResultValidation.ZeroExitCode` — ensures zero exit code when the process exits

**Default**: `CommandResultValidation.ZeroExitCode`.

**Examples**:

- Enable validation — will throw an exception if the process exits with a non-zero exit code:

```csharp
var cmd = Cli.Wrap("git")
    .WithValidation(CommandResultValidation.ZeroExitCode);
```

- Disable validation:

```csharp
var cmd = Cli.Wrap("git")
    .WithValidation(CommandResultValidation.None);
```

#### `WithStandardInputPipe(...)`

Sets the pipe source that will be used for the standard _input_ stream of the process.

**Default**: `PipeSource.Null`.

_Read more about this method in the [piping section](#piping)._

#### `WithStandardOutputPipe(...)`

Sets the pipe target that will be used for the standard _output_ stream of the process.

**Default**: `PipeTarget.Null`.

_Read more about this method in the [piping section](#piping)._

#### `WithStandardErrorPipe(...)`

Sets the pipe target that will be used for the standard _error_ stream of the process.

**Default**: `PipeTarget.Null`.

_Read more about this method in the [piping section](#piping)._

### Piping

**CliWrap** provides a very powerful and flexible piping model that allows you to redirect process's streams, transform input and output data, and even chain multiple commands together with minimal effort.
At its core, it's based on two abstractions: `PipeSource` which provides data for the standard input stream, and `PipeTarget` which reads data coming from the standard output stream or the standard error stream.

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

Alternatively, pipes can also be configured in a slightly terser way using pipe operators:

```csharp
await using var input = File.OpenRead("input.txt");
await using var output = File.Create("output.txt");

await (input | Cli.Wrap("foo") | output).ExecuteAsync();
```

Both `PipeSource` and `PipeTarget` have many factory methods that let you create pipe implementations for different scenarios:

- `PipeSource`:
  - `PipeSource.Null` — represents an empty pipe source
  - `PipeSource.FromStream(...)` — pipes data from any readable stream
  - `PipeSource.FromFile(...)` — pipes data from a file
  - `PipeSource.FromBytes(...)` — pipes data from a byte array
  - `PipeSource.FromString(...)` — pipes from a text string
  - `PipeSource.FromCommand(...)` — pipes data from the standard output of another command
- `PipeTarget`:
  - `PipeTarget.Null` — represents a pipe target that discards all data
  - `PipeTarget.ToStream(...)` — pipes data into any writable stream
  - `PipeTarget.ToFile(...)` — pipes data into a file
  - `PipeTarget.ToStringBuilder(...)` — pipes data as text into a `StringBuilder`
  - `PipeTarget.ToDelegate(...)` — pipes data as text, line-by-line, into an `Action<string>` or a `Func<string, Task>`
  - `PipeTarget.Merge(...)` — merges multiple outbound pipes by replicating the same data across all of them

> **Warning**:
> Using `PipeTarget.Null` results in the corresponding stream (stdout or stderr) not being opened for the underlying process at all.
> In the vast majority of cases, this behavior should be functionally equivalent to piping to a null stream, but without the performance overhead of consuming and discarding unneeded data.
> This may be undesirable in [certain situations](https://github.com/Tyrrrz/CliWrap/issues/145#issuecomment-1100680547) — in which case it's recommended to pipe to a null stream explicitly using `PipeTarget.ToStream(Stream.Null)`.

Below you can see some examples of what you can achieve with the help of **CliWrap**'s piping feature:

- Pipe a string into stdin:

```csharp
var cmd = "Hello world" | Cli.Wrap("foo");
await cmd.ExecuteAsync();
```

- Pipe stdout as text into a `StringBuilder`:

```csharp
var stdOutBuffer = new StringBuilder();

var cmd = Cli.Wrap("foo") | stdOutBuffer;
await cmd.ExecuteAsync();
```

- Pipe a binary HTTP stream into stdin:

```csharp
using var httpClient = new HttpClient();
await using var input = await httpClient.GetStreamAsync("https://example.com/image.png");

var cmd = input | Cli.Wrap("foo");
await cmd.ExecuteAsync();
```

- Pipe stdout of one command into stdin of another:

```csharp
var cmd = Cli.Wrap("foo") | Cli.Wrap("bar") | Cli.Wrap("baz");
await cmd.ExecuteAsync();
```

- Pipe stdout and stderr into stdout and stderr of the parent process:

```csharp
await using var stdOut = Console.OpenStandardOutput();
await using var stdErr = Console.OpenStandardError();

var cmd = Cli.Wrap("foo") | (stdOut, stdErr);
await cmd.ExecuteAsync();
```

- Pipe stdout into a delegate:

```csharp
var cmd = Cli.Wrap("foo") | Debug.WriteLine;
await cmd.ExecuteAsync();
```

- Pipe stdout into a file and stderr into a `StringBuilder`:

```csharp
var buffer = new StringBuilder();

var cmd = Cli.Wrap("foo") |
    (PipeTarget.ToFile("output.txt"), PipeTarget.ToStringBuilder(buffer));

await cmd.ExecuteAsync();
```

- Pipe stdout into multiple files simultaneously:

```csharp
var target = PipeTarget.Merge(
    PipeTarget.ToFile("file1.txt"),
    PipeTarget.ToFile("file2.txt"),
    PipeTarget.ToFile("file3.txt")
);

var cmd = Cli.Wrap("foo") | target;
await cmd.ExecuteAsync();
```

- Pipe a string into stdin of one command, stdout of that command into stdin of another command, and then stdout and stderr of the last command into stdout and stderr of the parent process:

```csharp
var cmd =
    "Hello world" |
    Cli.Wrap("foo").WithArguments("aaa") |
    Cli.Wrap("bar").WithArguments("bbb") |
    (Console.WriteLine, Console.Error.WriteLine);

await cmd.ExecuteAsync();
```

### Execution models

**CliWrap** provides a few high-level execution models that offer alternative ways to reason about commands.
These are essentially just extension methods that work by leveraging the [piping feature](#piping) shown earlier.

#### Buffered execution

This execution model lets you run a process while buffering its standard output and error streams in-memory as text.
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

By default, `ExecuteBufferedAsync()` assumes that the underlying process uses the default encoding (`Console.OutputEncoding`) for writing text to the console.
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

Besides executing a command as a task, **CliWrap** also supports an alternative model, in which the execution is represented as an event stream.
This lets you start a process and react to the events it produces in real-time.

Those events are:

- `StartedCommandEvent` — received just once, when the command starts executing (contains process ID)
- `StandardOutputCommandEvent` — received every time the underlying process writes a new line to the output stream (contains the text as a string)
- `StandardErrorCommandEvent` — received every time the underlying process writes a new line to the error stream (contains the text as a string)
- `ExitedCommandEvent` — received just once, when the command finishes executing (contains exit code)

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

> **Note**:
> Just like with `ExecuteBufferedAsync()`, you can specify custom encoding for `ListenAsync()` using one of its overloads.

#### Push-based event stream

Similarly to the pull-based stream, you can also execute a command as a _push-based_ event stream instead:

```csharp
using System.Reactive;
using CliWrap;
using CliWrap.EventStream;

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

Unlike the pull-based event stream, this execution model does not involve any back pressure, meaning that the data is pushed to the observer at the rate it becomes available.

> **Note**:
> Similarly to `ExecuteBufferedAsync()`, you can specify custom encoding for `Observe()` using one of its overloads.

#### Combining execution models with custom pipes

The different execution models shown above are based on the piping model, but those two concepts are not mutually exclusive.
When running a command using one of the built-in execution models, existing pipe configurations are preserved and extended using `PipeTarget.Merge(...)`.

This means that you can, for example, pipe a command to a file and simultaneously execute it as an event stream:

```csharp
var cmd =
    PipeSource.FromFile("input.txt") |
    Cli.Wrap("foo") |
    PipeTarget.ToFile("output.txt");

// Iterate as an event stream and pipe to a file at the same time
// (execution models preserve configured pipes)
await foreach (var cmdEvent in cmd.ListenAsync())
{
    // ...
}
```

### Timeout and cancellation

Command execution is asynchronous in nature as it involves a completely separate process.
In many cases, it may be useful to implement an abortion mechanism to stop the execution before it finishes, either through a manual trigger or a timeout.

To do that, issue the corresponding [`CancellationToken`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken) and include it when calling `ExecuteAsync()`:

```csharp
using System.Threading;
using CliWrap;

using var cts = new CancellationTokenSource();

// Cancel after a timeout of 10 seconds
cts.CancelAfter(TimeSpan.FromSeconds(10));

var result = await Cli.Wrap("path/to/exe").ExecuteAsync(cts.Token);
```

In the event of a cancellation request, the underlying process will be killed and `ExecuteAsync()` will throw an exception of type `OperationCanceledException` (or its derivative, `TaskCanceledException`).
You will need to catch this exception in your code to recover from cancellation:

```csharp
try
{
    await Cli.Wrap("path/to/exe").ExecuteAsync(cts.Token);
}
catch (OperationCanceledException)
{
    // Command was canceled
}
```

Besides outright killing the process, you can also request cancellation in a more graceful way by sending an interrupt signal.
To do that, pass an additional cancellation token to `ExecuteAsync()` that corresponds to that request:

```csharp
using var forcefulCts = new CancellationTokenSource();
using var gracefulCts = new CancellationTokenSource();

// Cancel forcefully after a timeout of 10 seconds
forcefulCts.CancelAfter(TimeSpan.FromSeconds(10));

// Cancel gracefully after a timeout of 7 seconds.
// If the process takes too long responding to our request,
// the previously configured cancellation will trigger
// after 3 seconds and forcefully kill the process.
gracefulCts.CancelAfter(TimeSpan.FromSeconds(7));

var result = await Cli.Wrap("path/to/exe").ExecuteAsync(forcefulCts.Token, gracefulCts.Token);
```

Requesting graceful cancellation in **CliWrap** is functionally equivalent to pressing `Ctrl+C` in the console window.
The underlying process may handle this signal to perform last-minute critical work before finally exiting on its own terms.

Graceful cancellation is inherently cooperative, so it's possible that the process may choose to ignore the request or take too long to fulfill it.
In the above example, this risk is mitigated by additionally scheduling forceful cancellation that prevents the command from hanging.

If you are executing a command inside a method where you don't want to expose those implementation details to the caller, you can rely on the following pattern to use the provided token for graceful cancellation and extend it with a timeout:

```csharp
public async Task GitPushAsync(CancellationToken cancellationToken = default)
{
    using var forcefulCts = new CancellationTokenSource();

    // When the cancellation token is triggered,
    // schedule forceful cancellation as fallback.
    await using var link = cancellationToken.Register(() =>
        forcefulCts.CancelAfter(TimeSpan.FromSeconds(3))
    );

    await Cli.Wrap("git")
        .WithArguments("push")
        .ExecuteAsync(forcefulCts.Token, cancellationToken);
}
```

> **Note**:
> Similarly to `ExecuteAsync()`, cancellation is also supported by `ExecuteBufferedAsync()`, `ListenAsync()`, and `Observe()`.

### Retrieving process-related information

The task returned by `ExecuteAsync()` and `ExecuteBufferedAsync()` is, in fact, not a regular `Task<T>`, but an instance of `CommandTask<T>`.
This is a specialized awaitable object that contains additional information about the process associated with the executing command:

```csharp
var task = Cli.Wrap("path/to/exe")
    .WithArguments("--foo bar")
    .ExecuteAsync();

// Get the process ID
var processId = task.ProcessId;

// Wait for the task to complete
await task;
```