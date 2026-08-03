# Invex .NET Repo Utils

> A collection of .NET utilities for building and maintaining .NET repositories.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![Build](https://github.com/Invex-Games/dotnet-repo-utils/actions/workflows/Build.yml/badge.svg)](https://github.com/Invex-Games/dotnet-repo-utils/actions/workflows/Build.yml)

`Invex.RepoUtils` bundles the tooling the Invex team uses to keep its .NET repositories
consistent, well-versioned, and safe to release. It ships three complementary pieces:

- A **Roslyn analyzer** that enforces explicit annotation of your public API surface.
- A **test utilities** library for snapshot-testing your public API surface.
- An **[Atom](https://github.com/Invex-Games/atom) build module** that adds reusable CI/CD targets
  for packing, testing, releasing, breaking-change detection, documentation generation, and Dependabot automation.

---

## Table of contents

- [Packages](#packages)
- [Invex.RepoUtils.PublicApiAnalyzers](#invexreputilspublicapianalyzers)
    - [Installation](#installation)
    - [Rules](#rules)
    - [Configuration](#configuration)
    - [Example](#example)
- [Invex.RepoUtils.TestUtils](#invexreputilstestutils)
    - [Installation](#installation-1)
    - [Usage](#usage)
- [Invex.RepoUtils.Atom.Module](#invexreputilsatommodule)
    - [Targets](#targets)
    - [Helpers](#helpers)
    - [Usage](#usage-1)
- [Repository structure](#repository-structure)
- [Building & testing](#building--testing)
- [CI/CD workflows](#cicd-workflows)
- [Versioning](#versioning)
- [Contributing](#contributing)
- [License](#license)

---

## Packages

| Package                              | Description                                                                                | Target           |
|--------------------------------------|--------------------------------------------------------------------------------------------|------------------|
| `Invex.RepoUtils.PublicApiAnalyzers` | Roslyn analyzer that flags public members not annotated as part of the public API surface. | `netstandard2.0` |
| `Invex.RepoUtils.TestUtils`         | Test utilities for snapshot-testing your assembly's public API surface.                    | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| `Invex.RepoUtils.Atom.Module`        | Atom build module providing pack/test/release, documentation, breaking-change, and Dependabot CI targets. | `net8.0`, `net9.0`, `net10.0` |

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

The analyzer recognizes `PublicAPI` and `PublicAPIAttribute` by name. The attribute itself is not
included in this package; add [JetBrains.Annotations](https://www.nuget.org/packages/JetBrains.Annotations)
or define an equivalent attribute in your project.

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
dotnet_code_quality.Invex_RepoUtils_PublicApiAnalyzers_ValidPublicApiAttributes = Experimental, MyCompanyApi
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

## Invex.RepoUtils.TestUtils

A test utility library that makes it easy to snapshot-test the public API surface of your
assemblies. It uses reflection to extract all public types and their members, serialises the result
to JSON, and pairs well with [Verify](https://github.com/VerifyTests/Verify) for approval-based
testing.

### Installation

```shell
dotnet add package Invex.RepoUtils.TestUtils
```

### Usage

Call `PublicApiSurfaceTestUtil.GetPublicApiSurface` with the assembly you want to inspect. The
returned JSON string can be verified with your preferred snapshot testing library:

```csharp
using Invex.RepoUtils.TestUtils;

[Test]
public Task PublicApiSurface()
{
    var surface = PublicApiSurfaceTestUtil.GetPublicApiSurface(typeof(MyLibType).Assembly);
    return Verify(surface);
}
```

The utility includes public types, properties, fields, and methods, while excluding non-public
members, special-name methods such as accessors and operators, and compiler-generated members.
Commit the resulting `*.verified.txt` file so API changes are visible in review and can be checked
for breaking changes in CI. For NUnit projects, `Verify.NUnit` is the recommended integration.

---

## Invex.RepoUtils.Atom.Module

An [Atom](https://github.com/Invex-Games/atom) build module that contributes reusable, opinionated
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
| `WaitForCopilotReview`      | `IWaitForCopilotReview`      | Blocks until GitHub Copilot has finished reviewing a PR (e.g. before enabling auto-merge). |

### Helpers

| Helper                         | Purpose                                                                                                                                                                                                                         |
|--------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `IApiSurfaceHelper`            | Diffs API definition files between two commits and classifies major/minor breaking changes.                                                                                                                                     |
| `IPrBreakingChangeHelper`      | Orchestrates the full PR breaking-change check against the latest release baseline.                                                                                                                                             |
| `IGithubPrHelper`              | Surfaces the GitHub pull-request number parameter for PR-scoped targets.                                                                                                                                                        |
| `INugetPackageUnlistHelper`    | Discovers superseded prereleases (or all prereleases below a given stable version) via the NuGet flat-container API and unlists them with resilient HTTP `DELETE` calls, writing a summary to the Atom build report.             |
| `IDocFxHelper`                 | Builds, serves, and publishes DocFX documentation to a project's `gh-pages` branch for GitHub Pages hosting.                                                                                                                     |
| `ICopilotReviewHelper`         | Polls a pull request until GitHub Copilot has finished reviewing it, failing on timeout.                                                                                                                                         |
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
including the `Validate`, `Build`, `Cleanup Prereleases`, and Dependabot auto-merge workflows.
The module also exposes targets for packing, testing, NuGet publishing, release creation,
prerelease cleanup, DocFX generation/publication, breaking-change checks, and waiting for Copilot
review.

---

## Repository structure

```
.
├── _atom/                                   # Atom build definition for this repo (IBuild.cs)
├── src/
│   ├── Invex.RepoUtils.Atom.Module/         # Atom CI/CD module (targets, helpers, models)
│   ├── Invex.RepoUtils.PublicApiAnalyzers/  # Roslyn public-API analyzer
│   └── Invex.RepoUtils.TestUtils/           # Test utilities for public API surface snapshots
├── tests/
│   ├── Invex.RepoUtils.Atom.Module.Tests/
│   ├── Invex.RepoUtils.PublicApiAnalyzers.Tests/
│   └── Invex.RepoUtils.TestUtils.Tests/
├── Directory.Build.props                    # Shared build settings
├── global.json                              # Required .NET SDK (10.0.0+, latest major roll-forward)
├── GitVersion.yml                           # Versioning configuration
└── Invex.RepoUtils.slnx                     # Solution
```

---

## Building & testing

The repository requires the .NET 10 SDK (see `global.json`) and uses C# 14, nullable reference types,
implicit usings, generated XML documentation, and `TreatWarningsAsErrors`.

```shell
# Restore and build the whole solution
dotnet build Invex.RepoUtils.slnx

# Run all tests for every target framework
dotnet test Invex.RepoUtils.slnx

# Run one framework when iterating locally
dotnet test Invex.RepoUtils.slnx --framework net10.0
```

Tests target .NET 8, .NET 9, and .NET 10. CI runs the test matrix for all three frameworks and also
packs the three NuGet projects.

### Documentation

The documentation site is built with DocFX. From the repository's Atom build definition:

```shell
dotnet run --project _atom/_atom.csproj -- BuildDocs
dotnet run --project _atom/_atom.csproj -- ServeDocs
```

See [`docs/`](docs/) and [`docs/contributing.md`](docs/contributing.md) for the complete
contributor guidance.

## CI/CD workflows

Workflow files under [`.github/workflows/`](.github/workflows/) are generated from
[`_atom/IBuild.cs`](_atom/IBuild.cs); do not edit them by hand. Regenerate them after changing
workflow definitions, targets, triggers, options, or injected parameters/secrets:

```shell
atom gen
# or
dotnet run --project _atom/_atom.csproj -- gen
```

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `Validate` | Pull requests into `main` or manual dispatch | Build, pack, test on .NET 8/9/10, check breaking changes, and wait for Copilot review |
| `Build` | Pushes to `main`, `feature/**`, or `patch/**`; releases; manual dispatch | Pack, test, publish to NuGet, create stable GitHub releases, and publish DocFX documentation |
| `Cleanup Prereleases` | Manual dispatch with a stable-version input | Unlist prerelease packages below the supplied version |
| `Dependabot Enable auto-merge` | Dependabot pull requests into `main` | Approve and enable auto-merge using the configured secret |

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
