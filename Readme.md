# CliWrap

[![Build](https://github.com/Tyrrrz/CliWrap/workflows/CI/badge.svg?branch=master)](https://github.com/Tyrrrz/CliWrap/actions)
[![Coverage](https://codecov.io/gh/Tyrrrz/CliWrap/branch/master/graph/badge.svg)](https://codecov.io/gh/Tyrrrz/CliWrap)
[![Version](https://img.shields.io/nuget/v/CliWrap.svg)](https://nuget.org/packages/CliWrap)
[![Downloads](https://img.shields.io/nuget/dt/CliWrap.svg)](https://nuget.org/packages/CliWrap)
[![Donate](https://img.shields.io/badge/patreon-donate-yellow.svg)](https://patreon.com/tyrrrz)
[![Donate](https://img.shields.io/badge/buymeacoffee-donate-yellow.svg)](https://buymeacoffee.com/tyrrrz)

CliWrap is a library that makes it easier to interact with command line interfaces. It provides a convenient wrapper around the target executable, allowing you to pass execution parameters and read the resulting output. The library can also handle errors reported by the underlying process, allows command cancellation and has both synchronous and asynchronous APIs.

## Download

- [NuGet](https://nuget.org/packages/CliWrap): `dotnet add package CliWrap`

## Features

- Full abstraction over `System.Diagnostics.Process`
- Execute commands in a synchronous, asynchronous, fire-and-forget manner
- Pass in command line arguments, standard input and environment variables
- Get process exit code, standard output and standard error as the result
- Abort the execution early using `System.Threading.CancellationToken`
- Set up callbacks that trigger when a process writes to standard output or error
- Custom encoding settings for standard input, output and error
- Fluent interface
- Targets .NET Framework 4.5+ and .NET Standard 2.0+
- No external dependencies

## Usage

### Execute a command

You can configure and execute CLI processes in a fluent manner. Replace `ExecuteAsync` with `Execute` if you want a blocking operation instead.

```c#
var result = await Cli.Wrap("cli.exe")
    .SetArguments("Hello world!")
    .ExecuteAsync();

var exitCode = result.ExitCode;
var stdOut = result.StandardOutput;
var stdErr = result.StandardError;
var startTime = result.StartTime;
var exitTime = result.ExitTime;
var runTime = result.RunTime;
```

### Argument list encoding

You can automatically encode command line arguments by passing them as a list.

```c#
var result = await Cli.Wrap("cli.exe")
    .SetArguments(new[] {"--option", "some value"})
    .ExecuteAsync();
```

### Standard input

You can pass stdin either as a string or a binary stream.

```c#
var result = await Cli.Wrap("cli.exe")
    .SetStandardInput("Hello world from stdin!")
    .ExecuteAsync();
```

### Environment variables

You can configure environment variables that will only be visible to the child process.

```c#
var result = await Cli.Wrap("cli.exe")
    .SetEnvironmentVariable("var1", "value1")
    .SetEnvironmentVariable("var2", "value2")
    .ExecuteAsync();
```

### Cancel execution

You can cancel execution at any point using `CancellationToken`, which will also kill the child process.

```c#
using (var cts = new CancellationTokenSource())
{
    cts.CancelAfter(TimeSpan.FromSeconds(5)); // e.g. timeout of 5 seconds

    var result = await Cli.Wrap("cli.exe")
        .SetCancellationToken(cts.Token)
        .ExecuteAsync();
}
```

### Callbacks for stdout and stderr

You can wire your own callbacks that will trigger on every line in stdout or stderr.

```c#
var result = await Cli.Wrap("cli.exe")
    .SetStandardOutputCallback(l => Console.WriteLine($"StdOut> {l}")) // triggered on every line in stdout
    .SetStandardErrorCallback(l => Console.WriteLine($"StdErr> {l}")) // triggered on every line in stderr
    .ExecuteAsync();
```

### Error handling

You can configure whether non-zero exit code or non-empty stderr should throw an exception.

```c#
var result = await Cli.Wrap("cli.exe")
    .EnableExitCodeValidation(true) // throw exception on non-zero exit code (on by default)
    .EnableStandardErrorValidation(true) // throw exception on non-empty stderr (off by default)
    .ExecuteAsync();
```

## Libraries used

- [ConfigureAwait.Fody](https://github.com/Fody/ConfigureAwait)
- [NUnit](https://github.com/nunit/nunit)
- [Coverlet](https://github.com/tonerdo/coverlet)

## Donate

If you really like my projects and want to support me, consider donating to me on [Patreon](https://patreon.com/tyrrrz) or [BuyMeACoffee](https://buymeacoffee.com/tyrrrz). All donations are optional and are greatly appreciated. 🙏