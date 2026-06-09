# Invex.RepoUtils.Atom.Module

An [Atom](https://github.com/DecSm/atom) build module that contributes reusable, opinionated CI/CD building blocks for .NET repositories. Add the interfaces you need to your Atom `IBuild` definition and wire the provided targets into your workflows.

## Overview

This module provides:

- **Targets** — Ready-to-use build targets that perform specific CI/CD actions (e.g., approve Dependabot PRs, check for breaking changes).
- **Helpers** — Shared logic interfaces that the targets consume (e.g., API surface diffing, GitHub PR context).
- **Models** — Data types representing breaking changes, releases, and diffs.
- **Extensions** — Injection options for wiring GitHub-specific values into workflows.

## Installation

```shell
dotnet add package Invex.RepoUtils.Atom.Module
```

The package targets `net8.0`, `net9.0`, and `net10.0`.

## Quick Start

Add the desired interfaces to your Atom build definition:

```csharp
[BuildDefinition]
[GenerateEntryPoint]
internal interface IBuild :
    IWorkflowBuildDefinition,
    IApproveDependabotPr,
    ICheckPrForBreakingChanges
{
    // Point the breaking-change check at your public API definition files.
    IEnumerable<RootedPath> ICheckPrForBreakingChanges.BreakingChangeFilesToCheck =>
        RootedFileSystem
            .Directory
            .GetFiles(RootedFileSystem.AtomRootDirectory / "tests", "*.verified.txt", SearchOption.AllDirectories)
            .Select(RootedFileSystem.CreateRootedPath);
}
```

## Learn More

- [Targets](targets.md) — available build targets
- [Helpers](helpers.md) — helper interfaces for custom builds
- [Breaking Change Detection](breaking-changes.md) — how the API surface check works
- [Dependabot Auto-Merge](dependabot.md) — automated dependency update merging

