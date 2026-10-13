# Introduction

Invex.RepoUtils is the shared tooling the Invex team uses to keep the public API of its .NET libraries
deliberate and its version numbers honest. This page explains what the three packages do, how they fit
together, and when they are (and are not) a good fit for your repository.

## What it is

Three packages that each solve one part of the same problem, "a public API change should never ship by
accident":

| Package | Role in the chain |
|---------|-------------------|
| `Invex.RepoUtils.PublicApiAnalyzers` | **Declare.** At compile time, every effectively-public member must be marked as intentional API. |
| `Invex.RepoUtils.TestUtils` | **Record.** A test snapshots the assembly's public surface, so any change shows up as a diff. |
| `Invex.RepoUtils.Atom.Module` | **Enforce.** In CI, the snapshot diff since the last release decides whether the version bump is big enough. |

The Atom module also carries the repository chores that every Invex library shares: enabling auto-merge on
Dependabot pull requests, waiting for GitHub Copilot's review, publishing DocFX sites to GitHub Pages and
unlisting superseded prerelease packages from NuGet.

## When to use it

- You publish NuGet libraries and want public API changes to be visible and reviewable in every pull request.
- You version with SemVer and want CI to refuse a release that removes API without a major bump.
- You already build with [Atom](https://github.com/Invex-Games/atom), or are willing to, and want ready-made
  targets instead of hand-written GitHub Actions YAML.

The packages are independent: the analyzer and the test utility work in any repository and with any test
framework, and only the CI pieces need Atom.

### When not to use it

- **Applications and internal tools.** If nothing consumes your assembly as a library, annotating its public
  surface adds noise without protecting anyone.
- **Exact binary-compatibility checks.** The snapshot records type names and member names, not signatures, and
  the breaking-change check is line-based. For signature-level compatibility, use the .NET SDK's package
  validation (`EnablePackageValidation`) or a dedicated API-compat tool alongside it.
- **CI systems other than GitHub Actions.** The Atom targets talk to the GitHub API (check runs, pull requests,
  GraphQL auto-merge) and assume GitHub-hosted repositories.

## Key concepts at a glance

| Concept | Meaning |
|---------|---------|
| **Effectively public** | Public all the way up the containing-type chain. Only these members are reported by `IPAA0001`. |
| **Valid attribute** | `[PublicAPI]` by default; more names can be added in `.editorconfig`. Annotating a type covers its members. |
| **API surface snapshot** | JSON listing each top-level public type with its public member names, committed as a `*.verified.txt` file. |
| **API definition files** | The text files the breaking-change check diffs; usually those snapshots. |
| **Baseline release** | The most recent `v{semver}` Git tag older than the current build version. |
| **Major / minor change** | A line removed from (major) or added to (minor) an API definition file since the baseline. |

## Design principles

- **Exposure is a decision.** Making something public should require an explicit, reviewable act, not just the
  `public` keyword.
- **Fail early, at the cheapest point.** The analyzer catches unintended exposure while you type; the snapshot
  catches it in the test run; the CI check catches the versioning mistake before merge.
- **Plain text as the contract.** Snapshots are ordinary files in the repository, so reviewers see API changes in
  the normal diff and tools need nothing more than `git diff`.
- **Composable building blocks.** Each Atom target and helper is an interface you mix into your build definition;
  take the ones you need and call the helpers from your own targets.
- **Few false positives.** The analyzer ignores constructors, accessors, overrides and compiler-generated members
  so that warnings point at real decisions.

## See also

- [Getting Started](getting-started.md)
- [Public API Analyzers](public-api-analyzers/index.md)
- [Test Utilities](test-utils/index.md)
- [Atom Build Module](atom-module/index.md)
- [API Reference](../api/index.md)
