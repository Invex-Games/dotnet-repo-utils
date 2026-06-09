# Invex .NET Repo Utils

> A collection of .NET utilities for building and maintaining .NET repositories.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/)

`Invex.RepoUtils` bundles the tooling the Invex team uses to keep its .NET repositories
consistent, well-versioned, and safe to release. It ships two complementary pieces:

- A **Roslyn analyzer** that enforces explicit annotation of your public API surface.
- An **[Atom](https://github.com/Invex-Games/atom) build module** that adds reusable CI/CD targets
  for packing, testing, releasing, breaking-change detection, and Dependabot automation.

---

## Table of contents

- [Packages](#packages)
- [Invex.RepoUtils.PublicApiAnalyzers](#invexreputilspublicapianalyzers)
    - [Installation](#installation)
    - [Rules](#rules)
    - [Configuration](#configuration)
    - [Example](#example)
- [Invex.RepoUtils.Atom.Module](#invexreputilsatommodule)
    - [Targets](#targets)
    - [Helpers](#helpers)
    - [Usage](#usage)
- [Repository structure](#repository-structure)
- [Building & testing](#building--testing)
- [Versioning](#versioning)
- [Contributing](#contributing)
- [License](#license)

---

## Packages

| Package                              | Description                                                                                | Target           |
|--------------------------------------|--------------------------------------------------------------------------------------------|------------------|
| `Invex.RepoUtils.PublicApiAnalyzers` | Roslyn analyzer that flags public members not annotated as part of the public API surface. | `netstandard2.0` |
| `Invex.RepoUtils.Atom.Module`        | Atom build module providing pack/test/release, breaking-change, and Dependabot CI targets. | `net10.0`        |

---

## Invex.RepoUtils.PublicApiAnalyzers

A Roslyn diagnostic analyzer that helps you keep an intentional public API surface. It reports
every effectively-public member that is **not** annotated with `[PublicAPI]` (or another attribute
you allow), so that exposing a new type or member is always a deliberate, reviewable decision.

### Installation

```shell
dotnet add package Invex.RepoUtils.PublicApiAnalyzers
```

The package is shipped as a development dependency (analyzer only) — it contributes no runtime
assemblies to your output.

### Rules

| Rule ID    | Category | Severity | Description                                                                                   |
|------------|----------|----------|-----------------------------------------------------------------------------------------------|
| `IPAA0001` | Design   | Warning  | Public member should be annotated with `[PublicAPI]` (or another configured valid attribute). |

The analyzer is attribute-aware and intentionally avoids false positives:

- It walks the containing-type chain, so a member is considered annotated when it — or any of its
  containing types — carries a valid attribute.
- Implicitly declared members, property/event accessors, constructors, and `override` members are
  ignored.
- A member is only flagged when it is **effectively public** (public all the way up its containing
  type chain).

### Configuration

By default the analyzer accepts `PublicAPI` / `PublicAPIAttribute`. You can extend the set of
attributes that satisfy the rule via an `.editorconfig` entry. Provide a comma-separated list of
attribute names (the `Attribute` suffix is optional — both forms are accepted):

```ini
# .editorconfig
[*.cs]
dotnet_code_quality.DecSm_Analyzers_ValidPublicApiAttributes = Experimental, MyCompanyApi
```

To change the severity of the rule:

```ini
[*.cs]
dotnet_diagnostic.IPAA0001.severity = error
```

### Example

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

---

## Invex.RepoUtils.Atom.Module

An [Atom](https://github.com/DecSM/atom) build module that contributes reusable, opinionated
CI/CD building blocks. Add the interfaces you need to your Atom `IBuild` definition and wire the
provided `Target`s into your workflows.

### Installation

```shell
dotnet add package Invex.RepoUtils.Atom.Module
```

### Targets

| Target                      | Interface                    | Purpose                                                                                   |
|-----------------------------|------------------------------|-------------------------------------------------------------------------------------------|
| `ApproveDependabotPr`       | `IApproveDependabotPr`       | Enables auto-merge on pull requests opened by `dependabot[bot]`.                          |
| `CheckPrForBreakingChanges` | `ICheckPrForBreakingChanges` | Detects public API breaking changes in a PR and reports the result as a GitHub check run. |

### Helpers

| Helper                         | Purpose                                                                                                                                                                                                                         |
|--------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `IApiSurfaceHelper`            | Diffs API definition files between two commits and classifies major/minor breaking changes.                                                                                                                                     |
| `IPrBreakingChangeHelper`      | Orchestrates the full PR breaking-change check against the latest release baseline.                                                                                                                                             |
| `IGithubPrHelper`              | Surfaces the GitHub pull-request number parameter for PR-scoped targets.                                                                                                                                                        |
| `DependabotEnableAutoMergePat` | Adds GitHub-specific injection options: `BuildOptions.Inject.Github.PullRequestNumber` (PR number from the event payload) and `BuildOptions.Inject.Github.DependabotEnableAutoMergePat` (the Dependabot auto-merge PAT secret). |

The breaking-change check compares the current build version against the most recent release tag
(`v{semver}`). It classifies removals from the public API surface as **major** changes and
additions as **minor** changes, then verifies the version has been bumped appropriately and posts
a pass/fail GitHub check run with a detailed summary.

### Usage

Add the desired interfaces to your build definition and reference the targets from a workflow:

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
    [
        // e.g. RootedFileSystem.AtomRootDirectory / "src/MyLib/PublicAPI.Shipped.txt",
    ];
}
```

See [`_atom/IBuild.cs`](_atom/IBuild.cs) for the full build definition used by this repository,
including the `Validate`, `Build`, and Dependabot auto-merge workflows.

---

## Repository structure

```
.
├── _atom/                                   # Atom build definition for this repo (IBuild.cs)
├── src/
│   ├── Invex.RepoUtils.Atom.Module/         # Atom CI/CD module (targets, helpers, models)
│   └── Invex.RepoUtils.PublicApiAnalyzers/  # Roslyn public-API analyzer
├── tests/
│   └── Invex.RepoUtils.PublicApiAnalyzers.Tests/
├── Directory.Build.props                    # Shared build settings
├── GitVersion.yml                           # Versioning configuration
└── Invex.RepoUtils.slnx                     # Solution
```

---

## Building & testing

The repository targets **.NET 10** and uses C# 14, with `TreatWarningsAsErrors` enabled.

```shell
# Restore & build the whole solution
dotnet build Invex.RepoUtils.slnx

# Run the analyzer test suite
dotnet test
```

The analyzer is validated across .NET 8, 9, and 10 reference assemblies in CI.

---

## Versioning

Versions are derived automatically by [GitVersion](https://gitversion.net/) using
[Conventional Commits](https://www.conventionalcommits.org/). The commit message prefix drives the
bump:

| Prefix                          | Bump  |
|---------------------------------|-------|
| `breaking:` / `major:`          | Major |
| `feat:` / `feature:` / `minor:` | Minor |
| `fix:` / `patch:`               | Patch |
| `semver-none` / `semver-skip`   | None  |

---

## Contributing

Contributions are welcome! Please:

1. Use Conventional Commit messages so versioning works correctly.
2. Annotate new public members with `[PublicAPI]` — the analyzer in this repo enforces it.
3. Add or update tests for analyzer changes.
4. Ensure `dotnet build` and `dotnet test` pass before opening a PR.

The `Validate` workflow runs the build, the test matrix, and the breaking-change check on every
pull request into `main`.

---

## License

Licensed under the [MIT License](LICENSE.txt). Copyright © 2026 Invex Games.
