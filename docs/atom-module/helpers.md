# Helpers

Helper interfaces provide the shared logic consumed by the [targets](targets.md). They can also be used directly in custom build definitions.

## IApiSurfaceHelper

Diffs API definition files between two commits and classifies the resulting changes.

### Interface

```csharp
public interface IApiSurfaceHelper : IBuildAccessor
```

### Methods

#### `IdentifyBreakingChanges`

```csharp
BreakingChanges IdentifyBreakingChanges(
    SemVer oldVersion,
    string oldCommitHash,
    SemVer newVersion,
    string newCommitHash,
    params IEnumerable<RootedPath> filesToCheck)
```

Compares the contents of the supplied files between two commits and determines which changes constitute breaking changes:

- **Major changes** — Lines were deleted from API definition files (API removals).
- **Minor changes** — Lines were added to API definition files (API additions) without removals.

The helper normalizes paths to repository-relative forward-slash form for comparison with Git diff output. Comma-only changes (reformatting) are excluded from major changes.

**Parameters:**

| Parameter | Description |
|-----------|-------------|
| `oldVersion` | The semantic version associated with the baseline commit. |
| `oldCommitHash` | The SHA of the baseline commit to compare from. |
| `newVersion` | The semantic version associated with the new commit. |
| `newCommitHash` | The SHA of the commit to compare to. |
| `filesToCheck` | The API definition files whose changes should be analyzed. |

**Returns:** A `BreakingChanges` instance with `MajorChanges` and `MinorChanges` lists.

---

## IGithubPrHelper

Exposes the GitHub pull request context that PR-scoped targets require.

### Interface

```csharp
public interface IGithubPrHelper : IBuildAccessor
```

### Members

| Member | Type | Description |
|--------|------|-------------|
| `GithubPullRequestNumber` | `int` | The number of the pull request being operated on. Injected from the GitHub Actions event payload. |

---

## IPrBreakingChangeHelper

Orchestrates the full PR breaking-change check against the latest release baseline.

### Interface

```csharp
public interface IPrBreakingChangeHelper : IApiSurfaceHelper
```

### Methods

#### `PerformPrBreakingChangeCheck`

```csharp
Task PerformPrBreakingChangeCheck(
    SemVer currentVersion,
    int pullRequestNumber,
    IEnumerable<RootedPath> filesToCheck,
    string githubToken,
    CancellationToken cancellationToken)
```

Runs the complete breaking change analysis:

1. Finds the most recent release tag (`v{semver}`) as the baseline.
2. Identifies breaking changes between the baseline and the current commit.
3. Validates that the version has been bumped appropriately.
4. Creates a GitHub check run with a `success` or `failure` conclusion.

#### `FindLatestReleaseInfo`

```csharp
ReleaseInfo? FindLatestReleaseInfo(Repository repo, SemVer currentVersion)
```

Finds the most recent release tag that represents a version older than the current version. Parses tags of the form `v{semver}` and returns the highest version that precedes the current one.

---

## IDocFxHelper

Generates, serves, and publishes [DocFX](https://dotnet.github.io/docfx/) documentation for the repository.

### Interface

```csharp
public interface IDocFxHelper : IDotnetCliHelper, IGithubHelper, ISetupBuildInfo
```

### Members

| Member | Type | Description |
|--------|------|-------------|
| `GeneratedDocsArtifactName` | `const string` | Name of the artifact and output sub-directory containing the generated DocFX site (`"GeneratedDocs"`). |

### Methods

#### `BuildDocFxDocs`

```csharp
Task BuildDocFxDocs(
    IEnumerable<RootedPath>? projectsToPrebuild = null,
    CancellationToken cancellationToken = default)
```

Builds the DocFX site and copies the generated output into the publish directory under `GeneratedDocsArtifactName`.

1. Optionally builds the supplied `projectsToPrebuild` in `Release` configuration so API metadata and XML docs are current.
2. On .NET 10+, runs the tool directly via `dotnet tool exec docfx`. On earlier runtimes, the DocFX global tool is updated/installed and then invoked via `dotnet docfx`.
3. The site is generated into the `_site` directory beneath the Atom root, then copied to the publish directory.

**Parameters:**

| Parameter | Description |
|-----------|-------------|
| `projectsToPrebuild` | Projects to build in `Release` before running DocFX. When `null`, no projects are pre-built. |
| `cancellationToken` | A token used to cancel the operation. |

#### `ServeDocFxDocs`

```csharp
Task ServeDocFxDocs(CancellationToken cancellationToken = default)
```

Serves the generated `_site` directory locally at `http://localhost:8080` for previewing until cancelled. Run `BuildDocFxDocs` first so the site exists. Cancellation is handled gracefully and shuts the server down without surfacing an error.

#### `PublishDocFxDocsToGithub`

```csharp
Task PublishDocFxDocsToGithub(
    string githubToken,
    string generatedDocsArtifactName,
    CancellationToken cancellationToken = default)
```

Publishes a previously generated site to the repository's `gh-pages` branch for GitHub Pages hosting:

1. Resolves the generated site artifact under the artifacts directory.
2. Creates a fresh orphan `gh-pages` branch in a temporary directory and commits the generated files.
3. Force-pushes to the `origin` remote, injecting the GitHub token as an `x-access-token` credential for HTTPS remotes.

The temporary checkout is always removed once the operation completes, even on failure.

**Parameters:**

| Parameter | Description |
|-----------|-------------|
| `githubToken` | The GitHub token used to authenticate the push. |
| `generatedDocsArtifactName` | The name of the artifact containing the generated site (typically `GeneratedDocsArtifactName`). |
| `cancellationToken` | A token used to cancel the operation. |

**Throws:** `StepFailedException` when the generated site artifact does not exist, or when the origin remote URL cannot be determined.

---

## Extension: InjectionOptionsExtensions

Provides GitHub-specific value injection options for build targets.

### Usage

Access via `BuildOptions.Inject.Github`:

```csharp
// Inject the pull request number from the GitHub event payload
BuildOptions.Inject.Github.PullRequestNumber

// Inject the Dependabot auto-merge PAT secret
BuildOptions.Inject.Github.DependabotEnableAutoMergePat
```

These are convenience wrappers that read values from the GitHub Actions event context and wire them as target parameters.

