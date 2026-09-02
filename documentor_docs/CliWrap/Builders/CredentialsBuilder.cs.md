# Technical Documentation: `CredentialsBuilder.cs`

## Overview

The `CredentialsBuilder` class in the `CliWrap.Builders` namespace implements the Builder design pattern to facilitate the step-by-step configuration of user credentials used when spawning a command-line process. It provides a fluent API for setting domain, username, password, and user profile loading options before instantiating an immutable `Credentials` object.

## Class Definition

```csharp
namespace CliWrap.Builders;

public class CredentialsBuilder
```

* **Namespace:** `CliWrap.Builders`
* **Dependencies:** `System.Diagnostics`

---

## Internal State

The builder maintains four private field variables representing the credential configuration state prior to construction:

| Field | Type | Default Value | Description |
| :--- | :--- | :--- | :--- |
| `_domain` | `string?` | `null` | Active Directory domain for process execution. |
| `_userName` | `string?` | `null` | The user account name under which the process will run. |
| `_password` | `string?` | `null` | The plain text password associated with the username. |
| `_loadUserProfile` | `bool` | `false` | Flag indicating whether the Windows user profile should be loaded. |

---

## Methods

All configuration methods return the current instance of `CredentialsBuilder` (`this`), enabling method chaining (Fluent Interface pattern).

### `SetDomain(string? domain)`

Sets the Active Directory domain to be used when starting the process.

* **Parameters:**
  * `domain` (`string?`): The domain name, or `null` if no domain is specified.
* **Returns:** `CredentialsBuilder` — The builder instance for method chaining.
* **Remarks:** Platform support and limitations correspond directly to `System.Diagnostics.ProcessStartInfo.Domain`.

```csharp
public CredentialsBuilder SetDomain(string? domain)
```

---

### `SetUserName(string? userName)`

Sets the username to be used when starting the process.

* **Parameters:**
  * `userName` (`string?`): The user name, or `null` if no user name is specified.
* **Returns:** `CredentialsBuilder` — The builder instance for method chaining.
* **Remarks:** Platform support and limitations correspond directly to `System.Diagnostics.ProcessStartInfo.UserName`.

```csharp
public CredentialsBuilder SetUserName(string? userName)
```

---

### `SetPassword(string? password)`

Sets the password associated with the specified username.

* **Parameters:**
  * `password` (`string?`): The user password, or `null` if no password is specified.
* **Returns:** `CredentialsBuilder` — The builder instance for method chaining.
* **Remarks:** Platform support and limitations correspond directly to `System.Diagnostics.ProcessStartInfo.Password`.

```csharp
public CredentialsBuilder SetPassword(string? password)
```

---

### `LoadUserProfile(bool loadUserProfile = true)`

Specifies whether the operating system should load the user's profile when starting the process.

* **Parameters:**
  * `loadUserProfile` (`bool`, optional): `true` to load the user profile; otherwise, `false`. Default is `true`.
* **Returns:** `CredentialsBuilder` — The builder instance for method chaining.
* **Remarks:** Platform support and limitations correspond directly to `System.Diagnostics.ProcessStartInfo.LoadUserProfile`.

```csharp
public CredentialsBuilder LoadUserProfile(bool loadUserProfile = true)
```

---

### `Build()`

Instantiates and returns a new `Credentials` object initialized with the values accumulated in the builder.

* **Parameters:** None.
* **Returns:** `Credentials` — A instance initialized with `(_domain, _userName, _password, _loadUserProfile)`.

```csharp
public Credentials Build()
```

---

## Example Usage

```csharp
using CliWrap.Builders;

var builder = new CredentialsBuilder();

Credentials credentials = builder
    .SetDomain("WORKGROUP")
    .SetUserName("Admin")
    .SetPassword("SecretP@ssword123")
    .LoadUserProfile(true)
    .Build();
```