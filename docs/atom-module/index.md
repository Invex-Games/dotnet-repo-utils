# Atom Build Module

`Invex.RepoUtils.Atom.Module` is an [Atom](https://github.com/Invex-Games/atom) build module that contributes reusable, opinionated CI/CD building blocks for .NET repositories. Add the interfaces you need to your Atom `IBuild` definition and wire the provided targets into your workflows. Use it when your repository builds with Atom and runs on GitHub Actions.

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

The package targets `net10.0`.

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

## What's in the box

| Interface | Kind | Purpose |
|-----------|------|---------|
| <xref:Invex.RepoUtils.Atom.Module.Targets.ICheckPrForBreakingChanges> | Target | Detects public API breaking changes in a PR and reports the result as a GitHub check run. |
| <xref:Invex.RepoUtils.Atom.Module.Targets.IApproveDependabotPr> | Target | Enables auto-merge on pull requests opened by `dependabot[bot]`. |
| <xref:Invex.RepoUtils.Atom.Module.Targets.IWaitForCopilotReview> | Target | Blocks until GitHub Copilot has finished reviewing a PR (e.g. before enabling auto-merge). |
| <xref:Invex.RepoUtils.Atom.Module.Helpers.IApiSurfaceHelper> | Helper | Diffs API definition files between two commits and classifies major/minor changes. |
| <xref:Invex.RepoUtils.Atom.Module.Helpers.IPrBreakingChangeHelper> | Helper | Orchestrates the full PR breaking-change check against the latest release baseline. |
| <xref:Invex.RepoUtils.Atom.Module.Helpers.IGithubPrHelper> | Helper | Surfaces the GitHub pull-request number parameter for PR-scoped targets. |
| <xref:Invex.RepoUtils.Atom.Module.Helpers.ICopilotReviewHelper> | Helper | Polls a pull request until GitHub Copilot has finished reviewing it, failing on timeout. |
| <xref:Invex.RepoUtils.Atom.Module.Helpers.IDocFxHelper> | Helper | Builds, serves and publishes DocFX documentation to the `gh-pages` branch. |
| <xref:Invex.RepoUtils.Atom.Module.Helpers.INugetPackageUnlistHelper> | Helper | Unlists superseded prereleases (or all prereleases below a stable version) via the NuGet API, with a build-report summary. |
| <xref:Invex.RepoUtils.Atom.Module.Extensions.InjectionOptionsExtensions> | Extension | Adds `BuildOptions.Inject.Github.PullRequestNumber` and `BuildOptions.Inject.Github.DependabotEnableAutoMergePat`. |

> [!NOTE]
> Packing, testing, NuGet publishing and uploading packages to a GitHub release are not targets of this module. Repositories
> define those targets themselves using Atom's own `Invex.Atom.Module.Dotnet` and `Invex.Atom.Module.GitHubWorkflows`
> helpers, as this repository's [`_atom/IBuild.cs`](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/_atom/IBuild.cs) does.

## See also

- [Targets](targets.md) — available build targets
- [Helpers](helpers.md) — helper interfaces for custom builds
- [Breaking Change Detection](breaking-changes.md) — how the API surface check works
- [Dependabot Auto-Merge](dependabot.md) — automated dependency update merging
- [API Reference](../../api/index.md)

