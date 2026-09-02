# Technical Documentation: `CliWrap.Builders.ArgumentsBuilder`

## Overview

The `ArgumentsBuilder` class in `CliWrap.Builders` is a fluent builder designed to format command-line arguments into a single, correctly delimited, and properly escaped string suitable for CLI execution.

It encapsulates internal string building logic via a `StringBuilder` and provides automatic escaping rules modeled after standard .NET command-line argument formatting guidelines (specifically referencing `System.Private.CoreLib`'s `PasteArguments.cs`).

---

## Class Architecture

- **Namespace:** `CliWrap.Builders`
- **Class Modifier:** `public partial class ArgumentsBuilder`
- **Design Pattern:** Fluent Builder Pattern (methods return `this` to allow chained calls).

### Internal Fields

| Field | Type | Description |
| :--- | :--- | :--- |
| `DefaultFormatProvider` | `static readonly IFormatProvider` | Set to `CultureInfo.InvariantCulture`. Used as the default formatting provider for `IFormattable` values when none is explicitly specified. |
| `_buffer` | `readonly StringBuilder` | Holds the accumulating sequence of arguments separated by spaces. |

---

## Key Features & Functionality

1. **Fluent Argument Appending (`Add` methods):**
   - Supports `string`, `IEnumerable<string>`, `IFormattable`, and `IEnumerable<IFormattable>`.
   - Delimits arguments automatically using spaces (via `_buffer.AppendIfNotEmpty(' ')`).
   - Defaults to escaping values unless explicitly specified otherwise (`bool escape = false`).

2. **Custom Format Provider Support:**
   - Accepts custom `IFormatProvider` or `CultureInfo` objects for formatting numeric, date, or other `IFormattable` argument values.

3. **Automatic Argument Escaping (`Escape` method):**
   - Automatically wraps strings containing whitespace or double quotes in quotes (`"`).
   - Correctly handles backslash (`\`) escaping preceding quotes or the end of the argument string.

---

## Method Reference

### 1. `Add(string value, bool escape)`
Appends a string argument to the buffer.
- **Parameters:**
  - `value`: The string value to append.
  - `escape`: If `true`, passes `value` through `Escape()` before appending.
- **Returns:** `ArgumentsBuilder` (`this`)

### 2. `Add(string value)`
Overload that appends a string argument with escaping enabled by default (`escape: true`).
- **Returns:** `ArgumentsBuilder` (`this`)

### 3. `Add(IEnumerable<string> values, bool escape)`
Appends a collection of string arguments sequentially.
- **Parameters:**
  - `values`: Sequence of string values.
  - `escape`: Whether each value in the sequence should be escaped.
- **Returns:** `ArgumentsBuilder` (`this`)

### 4. `Add(IEnumerable<string> values)`
Overload that appends a collection of strings with escaping enabled by default (`escape: true`).
- **Returns:** `ArgumentsBuilder` (`this`)

---

### `IFormattable` Overloads

These methods allow passing types implementing `IFormattable` (e.g., integers, floating-point numbers, `DateTime`) directly.

| Signature | Formatting Provider Used | Escaping |
| :--- | :--- | :--- |
| `Add(IFormattable value, IFormatProvider formatProvider, bool escape = true)` | User-provided `IFormatProvider` | Configurable (`true` by default) |
| `Add(IFormattable value, CultureInfo cultureInfo, bool escape)` | `cultureInfo` cast to `IFormatProvider` | Configurable |
| `Add(IFormattable value, CultureInfo cultureInfo)` | `cultureInfo` cast to `IFormatProvider` | `true` |
| `Add(IFormattable value, bool escape)` | `CultureInfo.InvariantCulture` | Configurable |
| `Add(IFormattable value)` | `CultureInfo.InvariantCulture` | `true` |

---

### `IEnumerable<IFormattable>` Overloads

Enables passing collections of `IFormattable` items directly.

| Signature | Formatting Provider Used | Escaping |
| :--- | :--- | :--- |
| `Add(IEnumerable<IFormattable> values, IFormatProvider formatProvider, bool escape = true)` | User-provided `IFormatProvider` | Configurable (`true` by default) |
| `Add(IEnumerable<IFormattable> values, CultureInfo cultureInfo, bool escape)` | `cultureInfo` cast to `IFormatProvider` | Configurable |
| `Add(IEnumerable<IFormattable> values, CultureInfo cultureInfo)` | `cultureInfo` cast to `IFormatProvider` | `true` |
| `Add(IEnumerable<IFormattable> values, bool escape)` | `CultureInfo.InvariantCulture` | Configurable |
| `Add(IEnumerable<IFormattable> values)` | `CultureInfo.InvariantCulture` | `true` |

---

### Finalization Method

#### `Build()`
Converts the populated internal buffer into a single formatted argument string.
- **Signature:** `public string Build()`
- **Returns:** `string` representing the command-line arguments.

---

## Static Utility: `Escape(string argument)`

### Signature
```csharp
public static string Escape(string argument)
```

### Purpose
Applies escaping logic to a string argument to guarantee that command-line processors interpret the value as a single discrete argument.

### Implementation Logic

1. **Short-Circuit Check:**
   If the argument length is greater than `0` and contains **no** whitespace characters or double quotes (`"`), the original string is returned unchanged.

2. **Quoting & Character Iteration:**
   If escaping is required, the string is enclosed in double quotes (`"`), and the string is parsed character by character:
   - **Backslashes (`\`):**
     - Counts consecutive backslashes `N`.
     - If the backslashes are at the **end of the string**, they are doubled to `N * 2` (so closing quotes are not escaped accidentally).
     - If followed immediately by a **double quote (`"`)**, they are converted to `N * 2 + 1` backslashes followed by an escaped quote (`\"`).
     - Otherwise, the backslashes are preserved as `N` backslashes.
   - **Quotes (`"`):**
     - Escaped as `\"`.
   - **Other Characters:**
     - Appended directly without modification.

---

## Code Maintenance Notes

The source code contains internal `TODO` inline annotations pointing to planned breaking changes in future versions:
- Redundant method overloads (specifically those taking explicit `CultureInfo` parameters or lacking default parameters) are marked for consolidation into overloads using optional parameters or `IFormatProvider`.