# Getting Started

This guide walks you through adding Invex.RepoUtils packages to your .NET project.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or .NET 8/9 for the Atom module)
- A .NET project targeting `net8.0`, `net9.0`, or `net10.0`

## Installation

### Public API Analyzer

The analyzer enforces explicit annotation of your public API surface. Install it as a development dependency:

```shell
dotnet add package Invex.RepoUtils.PublicApiAnalyzers
```

Once installed, the analyzer will automatically run during builds and flag any public members not annotated with `[PublicAPI]` or another configured attribute.

### Test Utilities

The test utilities package provides helpers for snapshot-testing your public API surface:

```shell
dotnet add package Invex.RepoUtils.TestUtils
```

This is intended for use in your test projects alongside a snapshot testing library like [Verify](https://github.com/VerifyTests/Verify).

### Atom Build Module

The Atom build module provides reusable CI/CD targets for your [Atom](https://github.com/DecSm/atom) build definitions:

```shell
dotnet add package Invex.RepoUtils.Atom.Module
```

## Quick Example

After installing the analyzer, annotate your public API:

```csharp
using JetBrains.Annotations;

// ✅ Annotated — this type is intentionally part of the public API
[PublicAPI]
public class MyService
{
    public string Name { get; set; }
    public void Execute() { }
}

// ⚠️ IPAA0001 — this public type is not annotated
public class InternalHelper { }
```

## Next Steps

- Learn about the [analyzer rules](public-api-analyzers/rules.md) and [configuration options](public-api-analyzers/configuration.md)
- Set up [snapshot testing for your API surface](test-utils/index.md)
- Add [CI/CD automation with Atom](atom-module/index.md)

