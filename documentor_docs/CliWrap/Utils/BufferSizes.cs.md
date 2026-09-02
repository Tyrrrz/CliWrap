# Technical Documentation: `CliWrap/Utils/BufferSizes.cs`

## Overview

The `CliWrap/Utils/BufferSizes.cs` file defines an internal utility class named `BufferSizes` within the `CliWrap.Utils` namespace. Its purpose is to centralize buffer size constant definitions used for stream and stream reader operations within the `CliWrap` assembly.

---

## Declaration & Accessibility

```csharp
namespace CliWrap.Utils;

internal static class BufferSizes
```

* **Namespace:** `CliWrap.Utils`
* **Access Modifier:** `internal` — Restricts access strictly to code within the same assembly (`CliWrap`). It is not exposed as part of the public API.
* **Class Type:** `static` — Indicates that the class cannot be instantiated or inherited. It serves purely as a container for constant fields.

---

## Member Definitions

The class contains two `public const int` fields:

| Constant Name | Type | Value | Description |
| :--- | :--- | :--- | :--- |
| `Stream` | `int` | `81920` | Defines the buffer size (in bytes) used for general stream operations (80 KB). |
| `StreamReader` | `int` | `1024` | Defines the buffer size (in bytes/characters) used for `StreamReader` operations (1 KB). |

---

## Component Details

### 1. `Stream`
```csharp
public const int Stream = 81920;
```
* **Value:** `81920` (80 KB)
* **Purpose:** Serves as a central constant value for stream I/O buffer allocations within the library.

### 2. `StreamReader`
```csharp
public const int StreamReader = 1024;
```
* **Value:** `1024` (1 KB)
* **Purpose:** Serves as a central constant value for `StreamReader` buffer allocations within the library.

---

## Summary

`CliWrap.Utils.BufferSizes` is a lightweight, internal static configuration class designed to maintain consistent, hardcoded buffer dimensions (`Stream` and `StreamReader`) across `CliWrap`'s stream-handling components.