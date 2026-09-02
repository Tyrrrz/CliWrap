# Technical Documentation: `EnvironmentVariablesBuilder.cs`

**File Path:** `CliWrap/Builders/EnvironmentVariablesBuilder.cs`  
**Namespace:** `CliWrap.Builders`  
**Class:** `EnvironmentVariablesBuilder`

---

## 1. Overview

The `EnvironmentVariablesBuilder` class provides a fluent builder pattern for constructing a collection of environment variables represented as key-value pairs (`string, string?`). It encapsulates a dictionary using exact ordinal string comparison and ensures state isolation by returning a fresh dictionary instance when `Build()` is called.

---

## 2. Key Components & Class Architecture

### Class Definition

```csharp
public class EnvironmentVariablesBuilder
```

### Internal State

```csharp
private readonly Dictionary<string, string?> _envVars = new(StringComparer.Ordinal);
```

* **`_envVars`**: A private, read-only dictionary that stores environment variable names (keys) and their corresponding values.
* **Key Comparer (`StringComparer.Ordinal`)**: Ensures variable names are treated with strict, case-sensitive ordinal comparison.
* **Nullable Values (`string?`)**: Values can be set to `null` or a string value.

---

## 3. Method Details

### `Set(string name, string? value)`

Sets a single environment variable with the specified name and value. If the key already exists in the builder, its value is overwritten.

```csharp
public EnvironmentVariablesBuilder Set(string name, string? value)
```

* **Parameters:**
  * `name` (`string`): The name of the environment variable (key).
  * `value` (`string?`): The value of the environment variable. Can be `null`.
* **Returns:** `EnvironmentVariablesBuilder` — Returns the current builder instance (`this`) to support method chaining.

---

### `Set(IEnumerable<KeyValuePair<string, string?>> variables)`

Sets multiple environment variables from an enumerable collection of key-value pairs.

```csharp
public EnvironmentVariablesBuilder Set(IEnumerable<KeyValuePair<string, string?>> variables)
```

* **Parameters:**
  * `variables` (`IEnumerable<KeyValuePair<string, string?>>`): A collection of key-value pairs representing environment variable names and values.
* **Behavior:** Iterates over each pair in `variables` and invokes `Set(name, value)` for each item.
* **Returns:** `EnvironmentVariablesBuilder` — Returns the current builder instance (`this`) to support method chaining.

---

### `Set(IReadOnlyDictionary<string, string?> variables)`

Overload that allows setting multiple environment variables from a read-only dictionary.

```csharp
public EnvironmentVariablesBuilder Set(IReadOnlyDictionary<string, string?> variables)
```

* **Parameters:**
  * `variables` (`IReadOnlyDictionary<string, string?>`): A read-only dictionary containing variable names and values.
* **Behavior:** Casts `variables` to `IEnumerable<KeyValuePair<string, string?>>` and delegates to the `IEnumerable` overload of `Set`.
* **Returns:** `EnvironmentVariablesBuilder` — Returns the current builder instance (`this`).

---

### `Build()`

Constructs and returns a snapshot of the configured environment variables.

```csharp
public IReadOnlyDictionary<string, string?> Build()
```

* **Returns:** `IReadOnlyDictionary<string, string?>` — A newly instantiated dictionary containing all configured key-value pairs.
* **Behavior:** Instantiates a new `Dictionary<string, string?>` initialized with the contents and equality comparer (`StringComparer.Ordinal`) of `_envVars`.
* **Design Note:** Returning a new copy prevents callers or consumers from mutating the internal `_envVars` instance stored within the builder after `Build()` is called.

---

## 4. Key Behavioral Characteristics

1. **Fluent Interface:** All `Set` methods return `this`, allowing multiple configuration calls to be chained sequentially.
2. **Case Sensitivity:** Uses `StringComparer.Ordinal` for dictionary key comparisons.
3. **Immutability of Builder State Output:** Calling `Build()` copies the contents into a new dictionary instance rather than exposing the underlying builder state.