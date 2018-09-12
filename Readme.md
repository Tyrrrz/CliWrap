# CliWrap

[![Build](https://img.shields.io/appveyor/ci/Tyrrrz/CliWrap/master.svg)](https://ci.appveyor.com/project/Tyrrrz/CliWrap)
[![Tests](https://img.shields.io/appveyor/tests/Tyrrrz/CliWrap/master.svg)](https://ci.appveyor.com/project/Tyrrrz/CliWrap)
[![NuGet](https://img.shields.io/nuget/v/CliWrap.svg)](https://nuget.org/packages/CliWrap)
[![NuGet](https://img.shields.io/nuget/dt/CliWrap.svg)](https://nuget.org/packages/CliWrap)

CliWrap is a library that makes it easier to interact with command line interfaces. It provides a convenient wrapper around the target executable, allowing you to pass execution parameters and read the resulting output. The library can also handle errors reported by the underlying process, allows command cancellation and has both synchronous and asynchronous APIs.

## Download

- Using NuGet: `Install-Package CliWrap`
- [Continuous integration](https://ci.appveyor.com/project/Tyrrrz/CliWrap)

## Features

- Full abstraction over `System.Diagnostics.Process`
- Execute commands in a synchronous, asynchronous, fire-and-forget manner
- Pass in command line arguments, standard input and environment variables
- Get process exit code, standard output and standard error as the result
- Abort the execution early using `System.Threading.CancellationToken`
- Set up callbacks that trigger when a process writes to standard output or error
- Custom encoding settings for standard input, output and error
- Fluent interface
- Targets .NET Framework 4.5+, .NET Core 1.0+ and .NET Standard 2.0+
- No external dependencies

## Usage

##### Execute a command

```c#
var result = await new Cli("cli.exe")
    .SetArguments("Hello world!")
    .ExecuteAsync();

var exitCode = result.ExitCode;
var stdOut = result.StandardOutput;
var stdErr = result.StandardError;
var startTime = result.StartTime;
var exitTime = result.ExitTime;
var runTime = result.RunTime;
```

##### Standard input

```c#
var result = await new Cli("cli.exe")
    .SetStandardInput("Hello world from stdin!") // can also pipe a stream instead
    .ExecuteAsync();
```

##### Environment variables

```c#
var result = await new Cli("cli.exe")
    .SetEnvironmentVariable("var1", "value1")
    .SetEnvironmentVariable("var2", "value2")
    .ExecuteAsync();
```

##### Cancel execution

```c#
using (var cts = new CancellationTokenSource())
{
    cts.CancelAfter(TimeSpan.FromSeconds(5)); // e.g. timeout of 5 seconds
    
    var result = await new Cli("cli.exe")
        .SetCancellationToken(cts.Token)
        .ExecuteAsync();                       
}
```

##### Callbacks for stdout and stderr

```c#
var result = await new Cli("cli.exe")
    .SetStandardOutputCallback(l => Console.WriteLine($"StdOut> {l}")) // triggered on every line in stdout
    .SetStandardErrorCallback(l => Console.WriteLine($"StdErr> {l}")) // triggered on every line in stderr
    .ExecuteAsync();
```

##### Ignore errors

```c#
var result = await new Cli("cli.exe")
    .EnableExitCodeValidation(false) // disables exceptions on non-zero exit code
    .EnableStandardErrorValidation(false) // disables exceptions on non-empty stderr
    .ExecuteAsync();
```

## Libraries used

- [NUnit](https://github.com/nunit/nunit)