# Technical Documentation: `CliWrap/Command.PipeOperators.cs`

## Overview

The `CliWrap/Command.PipeOperators.cs` file is a partial class definition for `Command` in the `CliWrap` namespace. Its primary purpose is to overload the C# bitwise OR operator (`|`) to provide a clean, shell-like syntax for piping standard input, standard output, and standard error streams between commands and various target/source types.

All operators defined in this file are marked with the `[Pure]` attribute, indicating that they do not mutate the existing `Command` instance, but instead return a new `Command` instance with modified configuration.

---

## Method & Operator Breakdown

The operator overloads in this file fall into four primary categories:
1. **Standard Output Piping (`Command | Target`)**
2. **Standard Output and Error Piping (`Command | (Target, Target)`)**
3. **Standard Input Piping (`Source | Command`)**
4. **Command-to-Command Piping (`Command | Command`)**

---

### 1. Standard Output Piping (`Command | Target`)

These overloads pipe the command's standard output (`stdout`) to a specified target.

| Operator Signature | Description | Underlying Logic |
| :--- | :--- | :--- |
| `operator |(Command source, PipeTarget target)` | Pipes standard output to a `PipeTarget`. | Calls `source.WithStandardOutputPipe(target)` |
| `operator |(Command source, Stream target)` | Pipes standard output to a `Stream`. | Wraps target with `PipeTarget.ToStream(target)` |
| `operator |(Command source, StringBuilder target)` | Pipes standard output to a `StringBuilder`. Uses `Encoding.Default`. | Wraps target with `PipeTarget.ToStringBuilder(target)` |
| `operator |(Command source, Func<string, CancellationToken, Task> target)` | Pipes standard output line-by-line to an async delegate accepting a `CancellationToken`. Uses `Encoding.Default`. | Wraps target with `PipeTarget.ToDelegate(target)` |
| `operator |(Command source, Func<string, Task> target)` | Pipes standard output line-by-line to an async delegate. Uses `Encoding.Default`. | Wraps target with `PipeTarget.ToDelegate(target)` |
| `operator |(Command source, Action<string> target)` | Pipes standard output line-by-line to a synchronous delegate. Uses `Encoding.Default`. | Wraps target with `PipeTarget.ToDelegate(target)` |

---

### 2. Combined Output & Error Piping (`Command | (Target, Target)`)

These overloads accept a `ValueTuple` to pipe both standard output (`stdout`) and standard error (`stderr`) simultaneously.

| Operator Signature | Description | Underlying Logic |
| :--- | :--- | :--- |
| `operator |(Command source, (PipeTarget stdOut, PipeTarget stdErr) targets)` | Pipes `stdout` and `stderr` to individual `PipeTarget` instances. | Calls `.WithStandardOutputPipe(targets.stdOut)` followed by `.WithStandardErrorPipe(targets.stdErr)` |
| `operator |(Command source, (Stream stdOut, Stream stdErr) targets)` | Pipes `stdout` and `stderr` to individual `Stream` instances. | Converts streams via `PipeTarget.ToStream` |
| `operator |(Command source, (StringBuilder stdOut, StringBuilder stdErr) targets)` | Pipes `stdout` and `stderr` to individual `StringBuilder` instances using `Encoding.Default`. | Converts builders via `PipeTarget.ToStringBuilder` |
| `operator |(Command source, (Func<string, CancellationToken, Task> stdOut, Func<string, CancellationToken, Task> stdErr) targets)` | Pipes `stdout` and `stderr` line-by-line to async delegates accepting a `CancellationToken`. Uses `Encoding.Default`. | Converts delegates via `PipeTarget.ToDelegate` |
| `operator |(Command source, (Func<string, Task> stdOut, Func<string, Task> stdErr) targets)` | Pipes `stdout` and `stderr` line-by-line to async delegates. Uses `Encoding.Default`. | Converts delegates via `PipeTarget.ToDelegate` |
| `operator |(Command source, (Action<string> stdOut, Action<string> stdErr) targets)` | Pipes `stdout` and `stderr` line-by-line to synchronous delegates. Uses `Encoding.Default`. | Converts delegates via `PipeTarget.ToDelegate` |

---

### 3. Standard Input Piping (`Source | Command`)

These overloads accept a source on the left-hand side and a `Command` on the right-hand side, redirecting the command's standard input (`stdin`).

| Operator Signature | Description | Underlying Logic |
| :--- | :--- | :--- |
| `operator |(PipeSource source, Command target)` | Pipes standard input from a `PipeSource`. | Calls `target.WithStandardInputPipe(source)` |
| `operator |(Stream source, Command target)` | Pipes standard input from a `Stream`. | Converts source via `PipeSource.FromStream(source)` |
| `operator |(ReadOnlyMemory<byte> source, Command target)` | Pipes standard input from a byte memory buffer. | Converts source via `PipeSource.FromBytes(source)` |
| `operator |(byte[] source, Command target)` | Pipes standard input from a byte array. | Converts source via `PipeSource.FromBytes(source)` |
| `operator |(string source, Command target)` | Pipes standard input from a string. Uses `Console.InputEncoding`. | Converts source via `PipeSource.FromString(source)` |

---

### 4. Command-to-Command Piping (`Command | Command`)

Pipes the standard output of one command directly into the standard input of another command.

| Operator Signature | Description | Underlying Logic |
| :--- | :--- | :--- |
| `operator |(Command source, Command target)` | Pipes `source` standard output into `target` standard input. | Wraps `source` using `PipeSource.FromCommand(source)` and pipes to `target` |

---

## Technical Considerations

### Character Encodings
- **Output Targets (`StringBuilder`, Delegates)**: Rely on `Encoding.Default` for decoding standard output and standard error bytes into text strings.
- **Input Sources (`string`)**: Rely on `Console.InputEncoding` for encoding text strings into bytes for standard input.

### Purity and Immutability
All overloaded operators in this file are decorated with the `[Pure]` attribute (`System.Diagnostics.Contracts.Pure`). This guarantees that operating on a `Command` instance via `|` produces a new immutable instance rather than modifying state in-place.