# Technical Documentation: `CliWrap/Credentials.cs`

## Overview

The `Credentials` class in `CliWrap` provides a data structure to encapsulate user authentication and identity settings used when launching external processes. It stores credentials such as username, domain, password, and user profile loading preferences, mapping closely to underlying system process start options (such as `System.Diagnostics.ProcessStartInfo`).

---

## Class Architecture

- **Namespace:** `CliWrap`
- **Class Modifiers:** `public partial class Credentials`

The class is defined across partial declarations within the file:
1. The main partial class definition containing constructors and instance properties.
2. A secondary partial class declaration containing static convenience properties.

---

## Constructors

### Primary Constructor

```csharp
public Credentials(
    string? domain = null,
    string? userName = null,
    string? password = null,
    bool loadUserProfile = false
)
```

The primary constructor initializes a new instance of `Credentials` with optional parameters.

#### Parameters

| Parameter | Type | Default Value | Description |
| :--- | :--- | :--- | :--- |
| `domain` | `string?` | `null` | Active Directory domain used for starting the process. |
| `userName` | `string?` | `null` | Username used for starting the process. |
| `password` | `string?` | `null` | Password associated with the username. |
| `loadUserProfile` | `bool` | `false` | Specifies whether to load the user profile when starting the process. |

---

### Overloaded Constructor (Legacy)

```csharp
[ExcludeFromCodeCoverage]
public Credentials(string? domain, string? username, string? password)
    : this(domain, username, password, false)
```

An additional constructor overload provided for backward compatibility. It accepts `domain`, `username`, and `password`, delegating to the primary constructor with `loadUserProfile` set to `false`.

- **Attributes:** `[ExcludeFromCodeCoverage]`
- **Note:** Marked in comments as a candidate for removal in a future breaking change.

---

## Properties

### Instance Properties

All instance properties are read-only (`get`-only) and set during initialization.

#### `Domain`
- **Type:** `string?`
- **Description:** Gets the Active Directory domain used when launching the process.
- **Platform Support:** Windows only.

#### `UserName`
- **Type:** `string?`
- **Description:** Gets the username used when launching the process.

#### `Password`
- **Type:** `string?`
- **Description:** Gets the password used when launching the process.
- **Platform Support:** Windows only.

#### `LoadUserProfile`
- **Type:** `bool`
- **Description:** Gets a value indicating whether the target user's profile should be loaded upon starting the process.
- **Platform Support:** Windows only.

---

### Static Properties

#### `Default`
- **Type:** `Credentials`
- **Access:** `public static Credentials Default { get; }`
- **Description:** Gets a default, empty instance of `Credentials` initialized with all default parameter values (`null` values for credentials and `false` for `loadUserProfile`).

---

## Platform-Specific Considerations

According to the XML documentation comments within the source file:
- `Domain`, `Password`, and `LoadUserProfile` depend on underlying `System.Diagnostics.ProcessStartInfo` capabilities and are **only supported on Windows** operating systems.