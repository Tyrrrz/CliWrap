# Technical Documentation: `AssertionExtensions.cs`

**File Path:** `CliWrap.Tests/Utils/Extensions/AssertionExtensions.cs`

---

## 1. Overview

The `AssertionExtensions` class is an internal static helper class located in the unit test suite (`CliWrap.Tests`). Its primary purpose is to extend standard `FluentAssertions` string assertions with custom assertion logic tailored for multi-line string validation.

Specifically, it introduces a method `ConsistOfLines` to split a subject string into individual lines (ignoring empty line entries) and verify that those lines match an expected sequence of strings.

---

## 2. Namespace and Dependencies

### Namespace
`CliWrap.Tests.Utils.Extensions`

### Dependencies
- **`System`**: Provides fundamental types, including `StringSplitOptions`.
- **`System.Collections.Generic`**: Provides `IEnumerable<T>` for handling sequence parameters.
- **`FluentAssertions`**: Provides fluent assertion capabilities (e.g., `.Should()`, `.Equal()`).
- **`FluentAssertions.Primitives`**: Contains primitive assertions, specifically `StringAssertions`.

---

## 3. Class Definition

```csharp
internal static class AssertionExtensions
```

- **Scope:** `internal` — Accessible only within the assembly containing `CliWrap.Tests`.
- **Modifier:** `static` — Contains extension methods and cannot be instantiated.

---

## 4. Extension Members

### `ConsistOfLines` Method

Extends `StringAssertions` (from `FluentAssertions`) to add line-by-line comparison capability.

#### Syntax
```csharp
public void ConsistOfLines(params IEnumerable<string> lines)
```

#### Parameters
| Parameter | Type | Description |
| :--- | :--- | :--- |
| `lines` | `params IEnumerable<string>` | A sequence or array of expected line strings to compare against the target string subject. |

#### Return Value
- `void`: Throws a `FluentAssertions` failure exception if the actual lines do not match the expected `lines`.

---

## 5. Execution Logic

When `ConsistOfLines` is called on a FluentAssertions string assertion (e.g., `stringSubject.Should()`):

1. **Access Target Subject**: Retrieves `assertions.Subject` (the raw string being tested).
2. **Split String**: Splits `assertions.Subject` using the line delimiter characters `'\n'` and `'\r'`.
3. **Filter Empty Lines**: Applies `StringSplitOptions.RemoveEmptyEntries` to exclude empty lines created by carriage returns or newlines.
4. **Assert Equality**: Evaluates the resulting string array against the expected `lines` parameter using `.Should().Equal(lines)`.

---

## 6. Code Summary

```csharp
using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Primitives;

namespace CliWrap.Tests.Utils.Extensions;

internal static class AssertionExtensions
{
    extension(StringAssertions assertions)
    {
        public void ConsistOfLines(params IEnumerable<string> lines) =>
            assertions
                .Subject.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                .Should()
                .Equal(lines);
    }
}
```