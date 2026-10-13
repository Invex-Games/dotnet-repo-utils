# Public API Analyzers

`Invex.RepoUtils.PublicApiAnalyzers` is a Roslyn diagnostic analyzer that helps you keep an intentional public API surface. It reports every effectively-public member that is **not** annotated with `[PublicAPI]` (or another attribute you allow), so that exposing a new type or member is always a deliberate, reviewable decision.

## Overview

The analyzer is shipped as a development dependency (analyzer only) — it contributes no runtime assemblies to your output. Once installed, it will automatically flag public members that haven't been explicitly marked as part of your public API.

## Key Features

- **Attribute-aware** — Walks the containing-type chain, so a member is considered annotated when it — or any of its containing types — carries a valid attribute.
- **Few false positives** — Implicitly declared members, property/event accessors, constructors, and `override` members are ignored.
- **Effective visibility** — A member is only flagged when it is **effectively public** (public all the way up its containing type chain).
- **Configurable** — The set of valid attributes can be extended via `.editorconfig`.
- **Concurrent** — Supports concurrent execution for fast analysis of large projects.

## Installation

```shell
dotnet add package Invex.RepoUtils.PublicApiAnalyzers
```

The package targets `netstandard2.0`, making it compatible with any .NET project.

> [!IMPORTANT]
> The analyzer recognizes `PublicAPI` and `PublicAPIAttribute` by name, but the attribute itself is not included in this package. Add [JetBrains.Annotations](https://www.nuget.org/packages/JetBrains.Annotations) or define an equivalent attribute in your project.

## Quick Example

```csharp
using JetBrains.Annotations;

// ⚠️ IPAA0001 — public type is not annotated.
public class Unmarked { }

// ✅ Annotated type — the type and all its public members are considered part of the API surface.
[PublicAPI]
public class Marked
{
    public int Value { get; set; }
}
```

## See also

- [Rules](rules.md) — detailed description of all diagnostic rules
- [Configuration](configuration.md) — how to customize which attributes are accepted and adjust severity
- [Test Utilities](../test-utils/index.md) — snapshot the API surface the analyzer helps you define

The analyzer has no consumable API, so it does not appear in the API reference.

