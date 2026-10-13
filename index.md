# Invex .NET Repo Utils

`Invex.RepoUtils` bundles the tooling the Invex team uses to keep its .NET repositories consistent,
well-versioned and safe to release: a Roslyn analyzer that makes every public API exposure deliberate,
a test utility that snapshots the public API surface, and an [Atom](https://github.com/Invex-Games/atom)
build module that turns those snapshots into CI checks and automates PR and package housekeeping.

## Start here

- [Introduction](docs/introduction.md): what the packages are for and how they fit together
- [Getting Started](docs/getting-started.md): from no API checks to a breaking-change gate in three steps
- [Public API Analyzers](docs/public-api-analyzers/index.md): the `IPAA0001` rule and its configuration
- [Atom Build Module](docs/atom-module/index.md): targets and helpers for CI
- [API Reference](api/index.md): types in `Invex.RepoUtils.TestUtils` and `Invex.RepoUtils.Atom.Module`

## Packages

| Package | What it does |
|---------|--------------|
| [`Invex.RepoUtils.PublicApiAnalyzers`](https://www.nuget.org/packages/Invex.RepoUtils.PublicApiAnalyzers) | Roslyn analyzer that flags public members not annotated as part of the public API surface. |
| [`Invex.RepoUtils.TestUtils`](https://www.nuget.org/packages/Invex.RepoUtils.TestUtils) | Test utilities for snapshot-testing your assembly's public API surface. |
| [`Invex.RepoUtils.Atom.Module`](https://www.nuget.org/packages/Invex.RepoUtils.Atom.Module) | Atom build module with breaking-change, Dependabot, Copilot-review, DocFX and NuGet-cleanup building blocks. |
