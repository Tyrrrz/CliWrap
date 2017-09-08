# CliWrap

Provides a wrapper around command line interface executables.

## Download

- Using nuget: `Install-Package CliWrap`

## Features

- Full abstraction over `System.Diagnostics.Process`
- Execute commands in a synchronous, asynchronous, fire-and-forget manner
- Pass in command line arguments as well as standard input
- Get process exit code, standard output and standard error as the result
- Stop the execution early using `System.Threading.CancellationToken`
- Targets .NET Framework 4.5+ and .NET Core 1.0+
- No external dependencies

## Usage

Execute a command and process output:
```c#
// Setup
var cli = new Cli("some_cli.exe");

// Execute
var output = await cli.ExecuteAsync("command --option");
// ... or in synchronous manner:
// var output = cli.Execute("command --option");

// Throw an exception if CLI reported an error
output.ThrowIfError();

// Process output
var code = output.ExitCode;
var stdOut = output.StandardOutput;
var stdErr = output.StandardError;
```

Execute a command without waiting for completion:
```c#
var cli = new Cli("some_cli.exe");
cli.ExecuteAndForget("command --option");
```

Pass in standard input:
```c#
var cli = new Cli("some_cli.exe");
var input = new ExecutionInput("command --option", "this is stdin");
var output = await cli.ExecuteAsync(input);
```

Cancel execution:
```c#
var cli = new Cli("some_cli.exe");

var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(1)); // e.g. timeout of 1 second

var output = await cli.ExecuteAsync("command --option", cts.Token);
```
