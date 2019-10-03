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

- Re-designed the API so that it follows the builder pattern. Execution parameters are now supplied using chainable methods on the `Cli` instead of via `ExecutionInput`. Things like `BufferHandler` and `CancellationToken` are now also configured in the same manner. *Refer to the readme to see updated usage examples.*
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
- Fixed an issue where `CancellationToken` would throw an out of scope exception when the process could not be killed.
- Fixed a race condition when an execution task is completed and canceled at the same time.

### v1.7.5 (21-Jan-2018)

- Added `EncodingSettings` to customize stream encoding.
- Added some ReSharper annotations to improve warnings and suggestions.

### v1.7.4 (10-Jan-2018)

- Execution start and exit times are now calculated separately, without accessing `Process` instance.
- Execution start and exit times are now `DateTimeOffset` instead of `DateTime`.
- Fixed an issue that prevented CliWrap from properly working on Linux.