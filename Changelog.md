### v3.4.4 (24-Apr-2022)

- Improved cancellation handling in all execution models by attaching the triggered `CancellationToken` to produced `OperationCanceledException`s.
- Fixed an issue where using `PipeSource.FromFile(...)` with a non-existing file did not throw an exception.

### v3.4.3 (18-Apr-2022)

- Added `PipeSource.Create(...)` and `PipeTarget.Create(...)` factory methods that can be used to create anonymous instances of `PipeSource` and `PipeTarget`. This approach can be convenient if you need to implement a custom `PipeSource` or `PipeTarget` but don't want to define a new class. (Thanks [@Cédric Luthi](https://github.com/0xced))
- Updated inline documentation for `PipeTarget.Null` with additional information clarifying the difference in behavior between that target and `PipeTarget.ToStream(Stream.Null)`.

### v3.4.2 (14-Mar-2022)

- Improved process execution performance by not setting `CreateNoWindow` to `false` when running inside a console application. This change reduces performance overhead by up to 60% in some cases. (Thanks [@Retik](https://github.com/Retik))
- Improved overall performance by making use of `IAsyncDisposable` on streams where it is available.
- Marked configuration methods on `Command` with the `Pure` attribute. The compiler will now produce a warning if you call any of these methods without using the returned value.

### v3.4.1 (30-Jan-2022)

- Fixed an issue where calling `Cli.Wrap(...).ExecuteAsync()` returned an invalid `CommandTask` (with `ProcessId` equal to `0`) when the process failed to start. It happened because the associated exception was thrown in an asynchronous context instead of getting propagated immediately. This issue caused event stream execution models (`Observe()` and `ListenAsync()`) to incorrectly yield `StartedCommandEvent` even if the process has not actually been able to start.

### v3.4 (07-Jan-2022)

- Added automatic resolving of script files on Windows (i.e. `.bat` and `.cmd` files). Previously, if you did `Cli.Wrap("foo")`, the underlying implementation of `System.Diagnostic.Process` would try to find `foo` in several locations, including directories listed in the PATH environment variable. On Windows, however, this only worked for `.exe` files, meaning that it wouldn't find `foo.cmd` or `foo.bat` even if they existed on the PATH. This was an issue with `Cli.Wrap("npm")` and `Cli.Wrap("az")` because those CLI tools are implemented as `.cmd` scripts on Windows. CliWrap now attempts to resolve those paths itself. (Thanks [@AliReZa Sabouri](https://github.com/alirezanet))
- Improved exception thrown when the underlying process fails to start. It now contains the target file path, which can be helpful to identify the exact command that failed. (Thanks [@Mohamed Hassan](https://github.com/moh-hassan))

⚠ Warning: the next major version of CliWrap (4.0) will drop support for legacy .NET Framework, .NET Standard, and .NET Core (prior to rebranding). Going forward, the library will only target .NET 5 and higher. If you have any questions, please comment on [this issue](https://github.com/Tyrrrz/CliWrap/issues/131).

### v3.3.3 (31-Aug-2021)

- Added an overload of `EnvironmentVariablesBuilder.Set(...)` that takes a dictionary parameter. This lets you set multiple environment variables at once by passing a dictionary when calling `Cli.Wrap("foo").WithEnvironmentVariables(env => ...)`.
- Added an overload of pipe operator for configuring a byte array as a pipe source.

### v3.3.2 (01-Apr-2021)

- Added the ability to remove an inherited environment variable by setting its value to `null`. (Thanks [@Ville Penttinen](https://github.com/vipentti))

### v3.3.1 (21-Feb-2021)

- Added `ExitCode` and `Command` properties to `CommandExecutionException`. These properties can be inspected to retrieve the exit code returned by the process and the command that triggered the exception. (Thanks [@Philip](https://github.com/pchinery))

### v3.3 (29-Dec-2020)

- Added `PipeSource.FromFile(...)` and `PipeTarget.ToFile(...)` as convenience shorthands for creating pipes that work with `FileStream` instances.
- Added overloads for `ArgumentsBuilder.Add(...)` that take `IFormatProvider` instead of `CultureInfo`. In a future major version, the overloads that take `CultureInfo` will be removed.
- Improved performance of `PipeSource.Null` and `PipeTarget.Null`.
- Signed assembly. (Thanks [@lazyboy1](https://github.com/lazyboy1))

Known incompatibility issue: Using CliWrap together with [ConfigureAwait.Fody](https://github.com/Fody/ConfigureAwait), in code compiled with .NET 5.0 SDK or higher, results in a `NullReferenceException` at run time. The recommended workaround is to call `ConfigureAwait(...)` on `CliWrap.CommandTask<T>` manually or to disable `ConfigureAwait.Fody` altogether.

### v3.2.4 (05-Dec-2020)

- Fixed an issue where `PipeTarget.ToDelegate(...)`, as well as execution models that depend on it (i.e. observable and async streams), didn't treat standalone `\r` character as a valid linebreak. (Thanks [@koryphaee](https://github.com/koryphaee))

### v3.2.3 (04-Nov-2020)

- Fixed an issue where `Command.ExecuteAsync(...)` sometimes threw `InvalidOperationException` indicating that the process has already exited. This problem only happened on Linux and macOS when the process exited too quickly.
- Improved performance and memory usage in async event stream execution model.

### v3.2.2 (15-Oct-2020)

- Fixed an issue where `Command.ExecuteAsync(...)` sometimes returned before the process actually exited, in case cancellation was requested. Now, this method only returns when the process has fully terminated or if the termination has failed for whatever reason.
- Fixed an issue where specifying password without also specifying domain resulted in the password being incorrectly ignored.

### v3.2.1 (14-Oct-2020)

- Improved performance and memory usage in all execution models. (Thanks [@Maarten Balliauw](https://github.com/maartenba) and [@Miha Zupan](https://github.com/MihaZupan))
- Added optimization for `PipeTarget.Merge(...)` that flattens inner targets in case they also represent merged targets.
- Added `CommandTask.ConfigureAwait(...)` as a shorthand for `CommandTask.Task.ConfigureAwait(...)`.

### v3.2 (17-Sep-2020)

- Added support for specifying user credentials when you want to run a command as a different user. This can be done by calling `cmd.WithCredentials(...)`. Domain and password options are only supported on Windows, while username can be set on any operating system. (Thanks [@Michi](https://github.com/MD-V))

### v3.1.1 (06-Sep-2020)

- Fixed an issue where `PipeTarget.Merge(...)` worked incorrectly when processing large stdout written in a single operation (more than 81920 bytes at a time). (Thanks [@Tom Pažourek](https://github.com/tompazourek))
- Replaced `[DebuggerDisplay]` attribute with an implementation of `ToString()` on types deriving from `CommandEvent`. Calling `commandEvent.ToString()` now yields an informative string that can be useful in debugging. This change shouldn't have any unwanted side effects.

### v3.1 (27-Jun-2020)

- Added an option to disable automatic argument escaping to `cmd.WithArguments(string[])` as well as `builder.Add(...)`. You can use it to escape the arguments yourself if the application requires it.
- Added an option to enable or disable auto-flushing for `PipeTarget.ToStream(...)` and `PipeSource.FromStream(...)`. If enabled, data will be copied as soon as it's available instead of waiting for the buffer to fill up. This is enabled by default, which is different from the previous behavior, although this change is not breaking for most scenarios.
- Fixed an issue where command execution threw an exception if the wrapped application didn't read stdin completely. The exception is now caught and ignored as it's not really an exceptional situation if the stdin contains excess data which is discarded by the wrapped application.
- Fixed an issue where command execution waited for piped stdin to resolve next bytes, even if the wrapped application didn't try to read them. This avoids unnecessary delay (which can be infinite if the stream never resolves) when the wrapped application doesn't need the rest of the stdin to complete execution.
- Fixed an issue where `PipeTarget.Merge(...)` worked incorrectly when used with `PipeTarget.ToDelegate(...)`, causing the latter to yield lines even where there were no line breaks.

### v3.0.3 (22-Jun-2020)

- Added error handling for when the internal call to `Process.Start()` returns `false`. This will now throw a more descriptive exception than it did previously.

### v3.0.2 (28-Apr-2020)

- Fixed an issue where piping stdin to an executable that also writes stdout caused a deadlock when the stdout was beyond a certain size.

### v3.0.1 (26-Apr-2020)

- Slightly improved performance by using `TaskCreationOptions.RunContinuationsAsynchronously` where appropriate. (Thanks [@Georg Jung](https://github.com/georg-jung))
- Added DebuggerDisplay attribute to derivatives of `CommandEvent` to aid in debugging. (Thanks [@Zoltán Lehóczky](https://github.com/Piedone))

### v3.0 (27-Feb-2020)

- Complete rework of the library.
- Added extensive support for piping.
- Multitude of improvements and breaking changes.

Refer to the [migration guide](<https://github.com/Tyrrrz/CliWrap/wiki/Migration-guide-(from-v2.5-to-v3.0)>) to see how you can update your old code to work with CliWrap v3.0.

Check out the new readme to see the whole list of new features.

### v2.5 (30-Oct-2019)

- Added callbacks that trigger when stdout/stderr streams are closed. (Thanks [@Daniel15](https://github.com/Daniel15))
- Added an overload for `SetCancellationToken` that accepts `killEntireProcessTree`. Passing `true` to this option will make CliWrap attempt to kill the entire process tree when cancellation is requested, as opposed to only the parent process. Note, this option is only available on .NET Framework 4.5+ and .NET Core 3.0+. (Thanks [@M-Patrone](https://github.com/M-Patrone))
- Removed ReSharper annotations.

### v2.4 (03-Oct-2019)

- Added `ProcessId` property to `Cli`. You can use it to get the ID of the underlying process as soon as it's started.

### v2.3.1 (10-Jul-2019)

- Fixed an issue where `Execute` and `ExecuteAsync` didn't return immediately after the execution was canceled.
- Fixed an issue where setting the same environment variable twice resulted in an error.
- Improved exception message in `ExitCodeValidationException` and `StandardErrorValidationException`.

### v2.3 (29-May-2019)

- Added an overload for `SetArguments` that takes a list. You can pass multiple arguments and they will be automatically encoded to preserve whitespace and other special characters.
- Fixed some typos in documentation.

### v2.2.2 (10-May-2019)

- Fixed an issue where `ExecuteAndForget` was throwing an exception if the underlying process outlived the execution of the method.

### v2.2.1 (01-Apr-2019)

- `ExitCodeValidationException` and `StandardErrorValidationException` now display both exit code and standard error inside the message. Useful when a process reported a non-zero exit code but the actual error message is in stderr.
- Removed `netcoreapp1.0` target.

### v2.2 (20-Dec-2018)

- Added `Cli.Wrap` static method to replace `new Cli()` for a more fluent interface. This also makes it so you're dealing with `ICli` instead of `Cli` throughout the entire method chain.
- Standard error validation is now disabled by default. This change was made because quite a few CLIs (e.g. git, ffmpeg) write progress to stderr.
- Changed `Execute` and `ExecuteAsync` to complete only after the process exits, regardless of cancellation. This fixes a problem where the underlying process could live for some brief moments after those methods returned, in case of cancellation.
- If `Execute` or `ExecuteAsync` is canceled, the underlying process will now be killed without waiting for standard input to write completely.
- Reworked underlying process handling to improve performance and maintainability. The `Execute` and `ExecuteAsync` methods are now virtually the same in terms of code, unlike before, where they were considerably different.

### v2.1 (15-Oct-2018)

- Added `ExecutionResult` to `ExitCodeValidationException` and `StandardErrorValidationException`. This way additional information can be inspected during debugging.
- `ExitCodeValidationException` and `StandardErrorValidationException` are now derived from `ExecutionResultValidationException`.
- Improved cancellation handling in synchronous workflow.
- Cancellation token is now used when writing stdin in asynchronous workflow.

### v2.0.1 (17-Sep-2018)

- Methods that used to return an instance of `Cli` now return `ICli` where applicable.

### v2.0 (12-Sep-2018)

- Re-designed the API so that it follows the builder pattern. Execution parameters are now supplied using chainable methods on the `Cli` instead of via `ExecutionInput`. Things like `BufferHandler` and `CancellationToken` are now also configured in the same manner. _Refer to the readme to see updated usage examples._
- It is now also possible to pipe a raw stream to standard input, instead of just a text string.
- Removed `ExecutionInput`, `CliSettings`, `BufferHandler`, `EncodingSettings`.
- Renamed `ExecutionOutput` to `ExecutionResult`.
- Removed `ExecutionResult.HasError` and `ExecutionResult.ThrowIfError()`.
- Added an option to automatically throw an exception when the underlying process reports a non-zero exit code. Is enabled by default.
- Added an option to automatically throw an exception when the underlying process writes anything to standard error. Is enabled by default.

### v1.8.5 (09-Jun-2018)

- Fixed exception messages not appearing in Visual Studio's exception popup.

### v1.8.4 (09-Mar-2018)

- `StandardErrorException` now shows the value of `StandardError` in `Message`.

### v1.8.3 (03-Mar-2018)

- Added `ICli` to aid in testing.

### v1.8.2 (02-Feb-2018)

- Made input model classes more accessible by removing immutability.
- `Cli` now throws exception if used after getting disposed.

### v1.8.1 (25-Jan-2018)

- Fixed another process leak when canceling synchronous `Execute`.

### v1.8 (25-Jan-2018)

- Refactored additional `Cli` constructor parameters into a separate class called `CliSettings`. This is breaking if you used to supply more than 1 parameter to the constructor.
- `Execute` and `ExecuteAsync` no longer depend on process getting successfully killed. An attempt to kill it is made but if it's not successful, no exception is thrown.
- All `CancellationToken` callbacks are now exception-safe.
- Fixed an issue where `CancellationToken` would throw an out-of-scope exception when the process could not be killed.
- Fixed a race condition when an execution task is completed and canceled at the same time.

### v1.7.5 (21-Jan-2018)

- Added `EncodingSettings` to customize stream encoding.
- Added some ReSharper annotations to improve warnings and suggestions.

### v1.7.4 (10-Jan-2018)

- Execution start and exit times are now calculated separately, without accessing `Process` instance.
- Execution start and exit times are now `DateTimeOffset` instead of `DateTime`.
- Fixed an issue that prevented CliWrap from properly working on Linux.
