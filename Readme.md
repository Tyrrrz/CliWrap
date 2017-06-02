# CliWrap

Provides a wrapper around command line interface executables

**Download:**

- Using nuget: `Install-Package CliWrap`

**Features:**

- Full abstraction over `System.Diagnostics.Process`
- Execute commands in a synchronous, asynchronous, fire-and-forget manner
- Pass in command line arguments as well as standard input
- Get process exit code, standard output and standard error as the result
- Targets .NET Framework 4.5+ and .NET Core 1.0+

**Usage examples:**

Execute a command and get standard output as a string:

````c#
var cli = new Cli("some_cli.exe");
string result = cli.Execute("verb --option").StandardOutput;
````

Execute a command asynchronously:
````c#
var cli = new Cli("some_cli.exe");
string result = (await cli.ExecuteAsync("verb --option")).StandardOutput;
````

Fire and forget:
````c#
var cli = new Cli("some_cli.exe");
cli.ExecuteAndForget("verb --option");
````

Process exit code and standard error:
````c#
var cli = new Cli("some_cli.exe");
var output = await cli.ExecuteAsync("verb --option");

int exitCode = output.ExitCode;
string stdErr = output.StandardError;
string stdOut = output.StandardOutput;

// Throw if the CLI reported an error
output.ThrowIfError();
````

Standard input:
````c#
var cli = new Cli("some_cli.exe");
var input = new ExecutionInput("verb --option", "Hello World");
var output = await cli.ExecuteAsync(input);
````