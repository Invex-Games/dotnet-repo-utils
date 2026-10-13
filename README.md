# Invex .NET Repo Utils

[![Build](https://github.com/Invex-Games/dotnet-repo-utils/actions/workflows/Build.yml/badge.svg)](https://github.com/Invex-Games/dotnet-repo-utils/actions/workflows/Build.yml)
[![NuGet](https://img.shields.io/nuget/v/Invex.RepoUtils.PublicApiAnalyzers.svg)](https://www.nuget.org/packages/Invex.RepoUtils.PublicApiAnalyzers)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-Standard%202.0%20%7C%208.0%20%7C%209.0%20%7C%2010.0-512BD4.svg)](https://dotnet.microsoft.com/)

`Invex.RepoUtils` bundles the tooling the Invex team uses to keep its .NET repositories consistent,
well-versioned and safe to release: a Roslyn analyzer that makes every public API exposure deliberate,
a test utility that snapshots the public API surface, and an [Atom](https://github.com/Invex-Games/atom)
build module that turns those snapshots into CI checks and automates PR and package housekeeping.

## Packages

| Package | What it does | Targets |
|---------|--------------|---------|
| [`Invex.RepoUtils.PublicApiAnalyzers`](https://www.nuget.org/packages/Invex.RepoUtils.PublicApiAnalyzers) | Roslyn analyzer that flags public members not annotated as part of the public API surface. | `netstandard2.0` |
| [`Invex.RepoUtils.TestUtils`](https://www.nuget.org/packages/Invex.RepoUtils.TestUtils) | Test utilities for snapshot-testing your assembly's public API surface. | `netstandard2.0`, `net10.0` |
| [`Invex.RepoUtils.Atom.Module`](https://www.nuget.org/packages/Invex.RepoUtils.Atom.Module) | Atom build module with breaking-change, Dependabot, Copilot-review, DocFX and NuGet-cleanup building blocks. | `net10.0` |

## Features

- **Intentional public API.** Rule `IPAA0001` reports every effectively-public member that is not annotated with `[PublicAPI]` or another attribute you allow.
- **API surface snapshots.** `PublicApiSurfaceTestUtil.GetPublicApiSurface` serialises an assembly's public types and member names to JSON for approval testing with Verify.
- **Breaking-change gate.** The `CheckPrForBreakingChanges` target diffs your API snapshot files against the latest `v{semver}` release and fails the PR check when the version bump is too small.
- **Dependabot auto-merge.** The `ApproveDependabotPr` target enables auto-merge on Dependabot pull requests with a dedicated PAT.
- **Wait for Copilot review.** The `WaitForCopilotReview` target blocks until GitHub Copilot has finished reviewing a pull request.
- **DocFX and NuGet housekeeping.** Helpers build, serve and publish a DocFX site to `gh-pages` and unlist superseded prerelease packages.

## Installation

```shell
dotnet add package Invex.RepoUtils.PublicApiAnalyzers   # in library projects
dotnet add package Invex.RepoUtils.TestUtils            # in test projects
dotnet add package Invex.RepoUtils.Atom.Module          # in your Atom build project (_atom)
```

- `Invex.RepoUtils.PublicApiAnalyzers` is a development dependency (analyzer only) and adds no runtime
  assemblies. It recognises `PublicAPI` / `PublicAPIAttribute` by name but does not ship the attribute: add
  [JetBrains.Annotations](https://www.nuget.org/packages/JetBrains.Annotations) or define an equivalent attribute.
- `Invex.RepoUtils.TestUtils` depends on JetBrains.Annotations (and System.Text.Json on `netstandard2.0`).
  It returns a JSON string, so pair it with a snapshot library such as [Verify](https://github.com/VerifyTests/Verify).
- `Invex.RepoUtils.Atom.Module` builds on `Invex.Atom.Module.GithubWorkflows`, `Invex.Atom.Module.Dotnet` and
  `Octokit.GraphQL`.

## Quick start

Annotate your public API, then snapshot it in a test:

```csharp
using JetBrains.Annotations;

// IPAA0001: Public member 'Unmarked' is missing [PublicAPI] or other valid attribute
public class Unmarked { }

// No diagnostic: the type and all its public members are part of the API surface.
[PublicAPI]
public class Marked
{
    public int Value { get; set; }
}
```

```csharp
using Invex.RepoUtils.TestUtils;

[TestFixture]
public class PublicApiTests
{
    [Test]
    public Task VerifyPublicApiSurface() =>
        VerifyJson(PublicApiSurfaceTestUtil.GetPublicApiSurface(typeof(Marked).Assembly));
}
```

Commit the resulting `*.verified.txt` file so every API change shows up in review.

## Usage

### Configure the analyzer

Extend the attributes that satisfy `IPAA0001` or change its severity in `.editorconfig`:

```ini
[*.cs]
dotnet_code_quality.Invex_RepoUtils_PublicApiAnalyzers_ValidPublicApiAttributes = Experimental, MyCompanyApi
dotnet_diagnostic.IPAA0001.severity = error
```

See [Public API Analyzers](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/docs/public-api-analyzers/index.md)
and its [rules](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/docs/public-api-analyzers/rules.md)
and [configuration](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/docs/public-api-analyzers/configuration.md) pages.

### Snapshot the public API surface

`GetPublicApiSurface` lists every top-level public type with its public member names, excluding accessors
and operators. The snapshot files double as the input to the breaking-change check.

See [Test Utilities](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/docs/test-utils/index.md).

### Gate pull requests on breaking changes

Add `ICheckPrForBreakingChanges` to your Atom build and point it at your snapshot files:

```csharp
[BuildDefinition]
[GenerateEntryPoint]
internal interface IBuild : IWorkflowBuildDefinition, ICheckPrForBreakingChanges
{
    IEnumerable<RootedPath> ICheckPrForBreakingChanges.BreakingChangeFilesToCheck =>
        RootedFileSystem
            .Directory
            .GetFiles(RootedFileSystem.AtomRootDirectory / "tests", "*.verified.txt", SearchOption.AllDirectories)
            .Select(RootedFileSystem.CreateRootedPath);
}
```

Removed lines count as major changes and added lines as minor changes; the target posts a pass/fail GitHub
check run. See [Breaking Change Detection](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/docs/atom-module/breaking-changes.md).

### Automate pull requests and releases

`IApproveDependabotPr`, `IWaitForCopilotReview`, `IDocFxHelper` and `INugetPackageUnlistHelper` cover
Dependabot auto-merge, waiting for Copilot, DocFX publishing and prerelease cleanup. See
[Atom Build Module](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/docs/atom-module/index.md),
[Targets](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/docs/atom-module/targets.md) and
[Helpers](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/docs/atom-module/helpers.md).

## Documentation

Full documentation: [https://invex-games.github.io/dotnet-repo-utils/](https://invex-games.github.io/dotnet-repo-utils/)

| Guide | Contents |
|-------|----------|
| [Introduction](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/docs/introduction.md) | What the packages are for, how they fit together, design principles |
| [Getting Started](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/docs/getting-started.md) | Install the packages and wire up the analyzer, snapshot test and CI check |
| [Public API Analyzers](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/docs/public-api-analyzers/index.md) | `IPAA0001`, its rules and `.editorconfig` configuration |
| [Test Utilities](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/docs/test-utils/index.md) | Snapshot-testing the public API surface |
| [Atom Build Module](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/docs/atom-module/index.md) | Targets, helpers, breaking-change detection, Dependabot auto-merge |
| [Contributing](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/docs/contributing.md) | Repository layout, build and test, CI workflows, PR process |
| [Versioning](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/docs/versioning.md) | GitVersion, commit prefixes, branches and releases |

## Development

The repository requires the .NET 10 SDK (see `global.json`).

```shell
dotnet build Invex.RepoUtils.slnx
dotnet test Invex.RepoUtils.slnx
```

Preview the documentation site with the Atom build definition:

```shell
atom BuildDocs
atom ServeDocs
# equivalently
dotnet run --project _atom/_atom.csproj -- ServeDocs
```

See [Contributing](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/docs/contributing.md) for the
repository layout, CI workflows and pull request process.

## License

Licensed under the [MIT License](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/LICENSE.txt). Copyright © 2026 Invex Games.
