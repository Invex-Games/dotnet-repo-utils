# Contributing

This page is for people changing Invex.RepoUtils itself: how the repository is laid out, how to build and
test it, how CI works and what a pull request needs. Contributions are welcome.

## Development setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (`global.json` requires 10.0.0 or later, rolling forward to the latest major)
- The .NET 8 and .NET 9 runtimes, to run the full test matrix locally
- Git
- A C# IDE (Rider, VS Code with C# Dev Kit, or Visual Studio)

The projects use C# 14, nullable reference types, implicit usings, generated XML documentation and
`TreatWarningsAsErrors`.

### Building and testing

```shell
# Clone the repository
git clone https://github.com/Invex-Games/dotnet-repo-utils.git
cd dotnet-repo-utils

# Restore and build the whole solution
dotnet build Invex.RepoUtils.slnx

# Run all tests for every target framework
dotnet test Invex.RepoUtils.slnx

# Run one framework when iterating locally
dotnet test Invex.RepoUtils.slnx --framework net10.0
```

Tests target .NET 8, .NET 9 and .NET 10. CI runs the test matrix for all three frameworks and also packs the
three NuGet projects.

### Building the documentation

The documentation site is built with DocFX through targets in the repository's Atom build definition:

```shell
atom BuildDocs
atom ServeDocs
# equivalently
dotnet run --project _atom/_atom.csproj -- BuildDocs
dotnet run --project _atom/_atom.csproj -- ServeDocs
```

`ServeDocs` builds the site first and serves it at `http://localhost:8080`.

### Repository structure

```text
.
├── _atom/                                    # Atom build definition for this repo (IBuild.cs)
├── src/
│   ├── Invex.RepoUtils.Atom.Module/          # Atom CI/CD module (targets, helpers, models)
│   ├── Invex.RepoUtils.PublicApiAnalyzers/   # Roslyn public-API analyzer
│   └── Invex.RepoUtils.TestUtils/            # Test utilities for public API surface snapshots
├── tests/
│   ├── Invex.RepoUtils.Atom.Module.Tests/
│   ├── Invex.RepoUtils.PublicApiAnalyzers.Tests/
│   └── Invex.RepoUtils.TestUtils.Tests/
├── docs/                                     # Documentation (this site)
├── Directory.Build.props                     # Shared build settings
├── global.json                               # Required .NET SDK (10.0.0+, latest major roll-forward)
├── GitVersion.yml                            # Versioning configuration
└── Invex.RepoUtils.slnx                      # Solution
```

## Guidelines

### 1. Conventional Commits

Use [Conventional Commit](https://www.conventionalcommits.org/) messages so versioning works correctly:

```text
feat: add new analyzer rule for internal types
fix: correct false positive on generic type parameters
breaking: rename IApiSurfaceHelper.Identify to IdentifyBreakingChanges
docs: update configuration guide for custom attributes
```

See [Versioning](versioning.md) for the full list of recognized prefixes.

### 2. Public API annotation

Annotate all new public members with `[PublicAPI]`. The analyzer in this repo enforces it, and with
`TreatWarningsAsErrors` an unannotated member fails the build.

```csharp
[PublicAPI]
public class MyNewFeature
{
    public void DoSomething() { }
}
```

### 3. Testing

- Add or update tests for any code changes, including analyzer changes.
- The analyzer tests run across .NET 8, 9 and 10 reference assemblies.
- Use [Verify](https://github.com/VerifyTests/Verify) for snapshot tests where appropriate. Public API changes
  update the `PublicApiTests.VerifyPublicApiSurface.verified.txt` snapshots, which feed the breaking-change check.
- Ensure `dotnet build` and `dotnet test` pass before opening a PR.

### 4. Documentation

- Update documentation for user-facing changes.
- Add XML doc comments to all public members.
- Update the README if adding new packages or features.

## Pull request process

1. Fork the repository and create a feature branch.
2. Make your changes following the guidelines above.
3. Ensure `dotnet build` and `dotnet test` pass locally.
4. Open a pull request into `main`.
5. The **Validate** workflow runs automatically: build and pack, the test matrix (net8.0, net9.0, net10.0),
   the breaking-change check, and a wait for GitHub Copilot's review.
6. Address any review feedback.

## CI/CD workflows

Workflow files under [`.github/workflows/`](https://github.com/Invex-Games/dotnet-repo-utils/tree/main/.github/workflows)
are generated from [`_atom/IBuild.cs`](https://github.com/Invex-Games/dotnet-repo-utils/blob/main/_atom/IBuild.cs).

> [!IMPORTANT]
> Do not edit the workflow YAML by hand. Regenerate it after changing workflow definitions, targets, triggers,
> options, or injected parameters and secrets, and commit it with the source change.

```shell
atom gen
# or
dotnet run --project _atom/_atom.csproj -- gen
```

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| **Validate** | Pull requests into `main` or manual dispatch | Build, pack, test on .NET 8/9/10, check breaking changes, and wait for Copilot review |
| **Build** | Pushes to `main`, `feature/**` or `patch/**`; releases; manual dispatch | Pack, test, publish to NuGet, unlist superseded prereleases, and (for stable versions) create GitHub releases and publish DocFX documentation |
| **Cleanup Prereleases** | Manual dispatch with a stable-version input | Unlist prerelease packages below the supplied version |
| **Dependabot Enable auto-merge** | Pull requests into `main` from `dependabot[bot]` | Enable auto-merge using the configured PAT secret |

## See also

- [Versioning](versioning.md)
- [Breaking Change Detection](atom-module/breaking-changes.md)
- [Atom Build Module](atom-module/index.md)
