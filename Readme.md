# CliWrap

[![Build status](https://img.shields.io/appveyor/ci/Tyrrrz/CliWrap.svg)](https://ci.appveyor.com/project/Tyrrrz/CliWrap)
[![Test status](https://img.shields.io/appveyor/tests/Tyrrrz/CliWrap.svg)](https://ci.appveyor.com/project/Tyrrrz/CliWrap/CliWrap/tests)
[![NuGet](https://img.shields.io/nuget/v/CliWrap.svg)](https://nuget.org/packages/CliWrap)
[![NuGet](https://img.shields.io/nuget/dt/CliWrap.svg)](https://nuget.org/packages/CliWrap)

CliWrap is a library that makes it easier to interact with command line interfaces. It provides a convenient wrapper around the target executable, allowing you to pass execution parameters and read the resulting output. The library can also handle errors reported by the underlying process, allows command cancellation and has both synchronous and asynchronous APIs.

## Download

- Using nuget: `Install-Package CliWrap`

## Features

- Full abstraction over `System.Diagnostics.Process`
- Execute commands in a synchronous, asynchronous, fire-and-forget manner
- Pass in command line arguments, standard input and environment variables
- Get process exit code, standard output and standard error as the result
- Stop the execution early using `System.Threading.CancellationToken`
- Kill all currently running processes when disposing, finalizing or on demand
- Targets .NET Framework 4.5+, .NET Core 1.0+ and .NET Standard 2.0+
- No external dependencies

## Usage

The `Cli` class was designed to have each instance treated as a member of your project. When you're wrapping around a command line interface, you can think of the interface itself as a singleton class and all its commands as methods of that class. Therefore, it is recommended to create a reusable instance of `Cli` for each target executable.

Executing processes are kept track of (except when launched using `ExecuteAndForget`) and killed when the `Cli` instance is disposed or finalized (which also happens when application exits).

##### Execute a command and handle output

```c#
using (var cli = new Cli("some_cli.exe"))
{
    // Execute
    var output = await cli.ExecuteAsync("command --option");
    // ... or in synchronous manner:
    // var output = cli.Execute("command --option");

    // Throw an exception if CLI reported an error
    output.ThrowIfError();

    // Extract output
    var code = output.ExitCode;
    var stdOut = output.StandardOutput;
    var stdErr = output.StandardError;
    var startTime = output.StartTime;
    var exitTime = output.ExitTime;
    var runTime = output.RunTime;
}
```

##### Execute a command without waiting for completion

```c#
using (var cli = new Cli("some_cli.exe"))
{
    cli.ExecuteAndForget("command --option");
}
```

##### Pass in standard input

```c#
using (var cli = new Cli("some_cli.exe"))
{
    var input = new ExecutionInput("command --option", "this is stdin");
    var output = await cli.ExecuteAsync(input);
}
```

##### Pass in environment variables

```c#
using (var cli = new Cli("some_cli.exe"))
{
    var input = new ExecutionInput("command --option");
    input.EnvironmentVariables.Add("some_var", "some_value");
    var output = await cli.ExecuteAsync(input);
}
```

##### Cancel execution

```c#
using (var cli = new Cli("some_cli.exe"))
using (var cts = new CancellationTokenSource())
{
    cts.CancelAfter(TimeSpan.FromSeconds(1)); // e.g. timeout of 1 second
    var output = await cli.ExecuteAsync("command --option", cts.Token);
}
```
