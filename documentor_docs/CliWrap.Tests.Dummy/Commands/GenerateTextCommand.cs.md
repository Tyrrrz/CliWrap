# Documentation: `GenerateTextCommand.cs`

## Overview

The `GenerateTextCommand` class is a CliFx command located within the `CliWrap.Tests.Dummy` namespace. Its primary purpose is to generate pseudo-random text of a specified length and split it across a designated number of lines, outputting the result to a specified target output stream (e.g., standard output). 

It uses a fixed random seed to ensure deterministic text generation across test runs.

---

## Command Metadata

- **Namespace**: `CliWrap.Tests.Dummy.Commands`
- **Command Name**: `generate text` (via `[Command("generate text")]`)
- **Interface Implemented**: `CliFx.ICommand`

---

## Fields and Constants

### `_random`
- **Type**: `Random`
- **Access**: `private readonly`
- **Value**: `new Random(1234567)`
- **Description**: A pseudo-random number generator initialized with a fixed seed (`1234567`). Using a fixed seed guarantees that generated text outputs are deterministic and reproducible during tests.

### `AllowedChars`
- **Type**: `char[]`
- **Access**: `private static readonly`
- **Value**: Printable ASCII characters generated using `Enumerable.Range(32, 94)` (ASCII codes 32 through 125).
- **Description**: Defines the set of characters available for random text generation.

---

## Command Options (Properties)

### `Target`
- **Attribute**: `[CommandOption("target")]`
- **Type**: `OutputTarget`
- **Default Value**: `OutputTarget.StdOut`
- **Description**: Specifies the output stream target where the generated text will be written.

### `Length`
- **Attribute**: `[CommandOption("length")]`
- **Type**: `int`
- **Default Value**: `100_000`
- **Description**: The total number of characters to generate.

### `LinesCount`
- **Attribute**: `[CommandOption("lines")]`
- **Type**: `int`
- **Default Value**: `1`
- **Description**: The number of lines into which the total character count should be divided.

---

## Methods

### `ExecuteAsync(IConsole console)`

Executes the text generation command asynchronously.

- **Parameters**: 
  - `console` (`IConsole`): The CliFx console abstraction used to obtain stream writers for outputting data.
- **Return Type**: `ValueTask`

#### Execution Workflow:

1. **Validation Check**:
   - If `Length` is less than or equal to `0`, or `LinesCount` is less than or equal to `0`, the method terminates early without producing output.

2. **Line Length Calculation**:
   - Calculates the base character length per line: `lineLength = Length / LinesCount`.

3. **Text Generation and Writing Loop**:
   - Iterates from line index `0` up to `LinesCount - 1`.
   - **Current Line Length Calculation**:
     - For all lines except the last line, the character count is set to `lineLength`.
     - For the last line, the character count is calculated as `Length - lineLength * lineNumber`. This places any remainder characters into the last line so that the total character count accurately equals `Length`.
   - **String Creation**:
     - Uses `string.Create(...)` combined with `random.GetItems(AllowedChars, buffer)` to fill the character buffer randomly from `AllowedChars`.
   - **Output Stream Writing**:
     - Retrieves writers for the configured `Target` via `console.GetWriters(Target)`.
     - Asynchronously writes the line to each writer using `writer.WriteLineAsync(line)`.