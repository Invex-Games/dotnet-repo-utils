# Contributing

Contributions to Invex.RepoUtils are welcome! Please follow these guidelines to ensure a smooth review process.

## Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Git
- A C# IDE (Rider, VS Code with C# Dev Kit, or Visual Studio)

### Building

```shell
# Clone the repository
git clone https://github.com/Invex-Games/dotnet-repo-utils.git
cd dotnet-repo-utils

# Restore & build
dotnet build Invex.RepoUtils.slnx

# Run all tests
dotnet test
```

### Project Structure

```
.
├── _atom/                                    # Atom build definition (IBuild.cs)
├── src/
│   ├── Invex.RepoUtils.Atom.Module/          # Atom CI/CD module
│   ├── Invex.RepoUtils.PublicApiAnalyzers/   # Roslyn analyzer
│   └── Invex.RepoUtils.TestUtils/            # Test utilities
├── tests/
│   ├── Invex.RepoUtils.Atom.Module.Tests/
│   ├── Invex.RepoUtils.PublicApiAnalyzers.Tests/
│   └── Invex.RepoUtils.TestUtils.Tests/
├── docs/                                     # Documentation (this site)
├── Directory.Build.props                     # Shared build settings
├── GitVersion.yml                            # Versioning configuration
└── Invex.RepoUtils.slnx                      # Solution
```

## Guidelines

### 1. Conventional Commits

Use [Conventional Commit](https://www.conventionalcommits.org/) messages so versioning works correctly:

```
feat: add new analyzer rule for internal types
fix: correct false positive on generic type parameters
breaking: rename IApiSurfaceHelper.Identify to IdentifyBreakingChanges
docs: update configuration guide for custom attributes
```

See [Versioning](versioning.md) for the full list of recognized prefixes.

### 2. Public API Annotation

Annotate all new public members with `[PublicAPI]`. The analyzer in this repo enforces it — your build will produce warnings otherwise.

```csharp
[PublicAPI]
public class MyNewFeature
{
    public void DoSomething() { }
}
```

### 3. Testing

- Add or update tests for any code changes.
- The analyzer tests run across .NET 8, 9, and 10 reference assemblies.
- Use [Verify](https://github.com/VerifyTests/Verify) for snapshot tests where appropriate.
- Ensure `dotnet test` passes before opening a PR.

### 4. Documentation

- Update documentation for user-facing changes.
- Add XML doc comments to all public members.
- Update the README if adding new packages or features.

## Pull Request Process

1. Fork the repository and create a feature branch.
2. Make your changes following the guidelines above.
3. Ensure `dotnet build` and `dotnet test` pass locally.
4. Open a pull request into `main`.
5. The **Validate** workflow will run automatically, which includes:
   - Build verification
   - Test matrix (net8.0, net9.0, net10.0)
   - Breaking change analysis
6. Address any review feedback.

## CI/CD

The repository uses Atom for CI/CD with the following workflows:

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| **Validate** | PRs into `main` | Build, test (multi-framework matrix), and breaking change check |
| **Build** | Push to `main`, `feature/**`, `patch/**`, and releases | Build, test, pack, push to NuGet, and create GitHub releases |
| **Dependabot** | PRs from `dependabot[bot]` | Enables auto-merge on dependency update PRs |

