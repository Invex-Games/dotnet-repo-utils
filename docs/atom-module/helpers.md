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

## INugetPackageUnlistHelper

Discovers and unlists prerelease packages that have been superseded by a newly published version.

### Interface

```csharp
public interface INugetPackageUnlistHelper : IReportsHelper
```

### Methods

#### `SelectSupersededPrereleases`

```csharp
static IReadOnlyList<SemVer> SelectSupersededPrereleases(
    SemVer currentVersion,
    IEnumerable<SemVer> publishedVersions)
```

Selects the published versions superseded by `currentVersion`: every version that shares the same
`SemVer.Prefix` (core `MAJOR.MINOR.PATCH`), is a prerelease, and has lower SemVer precedence than the
current version. Results are returned in ascending precedence order.

- A **stable** `currentVersion` matches **all** prereleases of the same core version (for example,
  `1.1.0` selects `1.1.0-alpha.1`, `1.1.0-beta.2`, `1.1.0-rc.1`).
- A **prerelease** `currentVersion` matches only **earlier** prereleases of the same core version (for
  example, `1.1.0-rc.2` selects `1.1.0-beta.1` and `1.1.0-rc.1`, but not `1.1.0-rc.3` or `1.1.0`).
- Versions of a different core (`1.0.1-rc.1`), equal/higher versions, and the current version itself are
  never selected.

#### `SelectPrereleasesBelowVersion`

```csharp
static IReadOnlyList<SemVer> SelectPrereleasesBelowVersion(
    SemVer version,
    IEnumerable<SemVer> publishedVersions)
```

Selects every published **prerelease** whose SemVer precedence is strictly below `version`, in
ascending precedence order. Stable versions and prereleases at or above the given version are never
selected. For example, a `version` of `2.0.0` selects every prerelease below `2.0.0` (such as
`1.5.0-rc.1` and `2.0.0-rc.1`) while leaving all stable versions and every prerelease at or above
`2.0.0` untouched. This is intended for cleaning up obsolete prerelease packages left behind once a
newer stable version has shipped.

#### `UnlistSupersededPrereleasesForPackages`

```csharp
Task<IReadOnlyList<UnlistResult>> UnlistSupersededPrereleasesForPackages(
    string feedUrl,
    string apiKey,
    IEnumerable<string> packageIds,
    SemVer currentVersion,
    CancellationToken cancellationToken)
```

Unlists every superseded prerelease for the supplied package ids and writes a summary to the Atom
build report:

1. Reads the feed's service index and resolves the `PackageBaseAddress/3.0.0` (flat container) and
   `PackagePublish/2.0.0` resources. When either is missing, the work is skipped and reported.
2. For each package, reads the published versions from the flat-container API and selects the
   superseded prereleases via `SelectSupersededPrereleases`.
3. Unlists each selected version with an HTTP `DELETE` (authenticated with the `X-NuGet-ApiKey`
   header). Transient failures are retried with exponential backoff; a `404` is treated as
   already-unlisted and skipped.
4. Adds a `TableReportData` summary (or a `TextReportData` note when nothing was unlisted) to the
   build report.

**Throws:** `StepFailedException` when one or more versions could not be unlisted after retries.

#### `UnlistPrereleasesBelowVersionForPackages`

```csharp
Task<IReadOnlyList<UnlistResult>> UnlistPrereleasesBelowVersionForPackages(
    string feedUrl,
    string apiKey,
    IEnumerable<string> packageIds,
    SemVer version,
    CancellationToken cancellationToken)
```

Unlists every prerelease whose SemVer precedence is strictly below `version` (typically a stable
version) for the supplied package ids, writing the same kind of build-report summary as
`UnlistSupersededPrereleasesForPackages`. It uses the same feed-resource discovery, retry/backoff, and
reporting pipeline but selects versions with `SelectPrereleasesBelowVersion`. This is intended for a
**manually-triggered** cleanup of old prerelease packages rather than the automatic per-publish
supersedence cleanup.

**Throws:** `StepFailedException` when one or more versions could not be unlisted after retries.

### Usage

The module does not ship a ready-made target for this helper — wire it into your own target so it
runs after your package-publish target:

```csharp
internal interface IBuild : INugetPackageUnlistHelper, ISetupBuildInfo /* , ... */
{
    Target UnlistSupersededPrereleases =>
        t => t
            .DescribedAs("Unlists prerelease packages superseded by the just-published version.")
            .RequiresParam(nameof(NugetFeed), nameof(NugetApiKey))
            .ConsumesVariable(nameof(SetupBuildInfo), nameof(BuildVersion))
            .DependsOn(nameof(PushToNuget))
            .Executes(cancellationToken =>
                UnlistSupersededPrereleasesForPackages(
                    NugetFeed,
                    NugetApiKey,
                    ProjectsToPack,
                    BuildVersion,
                    cancellationToken));
}
```

For one-off housekeeping, wire `UnlistPrereleasesBelowVersionForPackages` into a manually-triggered
(`workflow_dispatch`) target to bulk-unlist old prereleases below a given stable version:

```csharp
internal interface IBuild : INugetPackageUnlistHelper /* , ... */
{
    [ParamDefinition("prerelease-cleanup-below-version",
        "Unlist all prerelease packages below this stable version.")]
    string PrereleaseCleanupBelowVersion => GetParam(() => PrereleaseCleanupBelowVersion)!;

    Target CleanupOldPrereleases =>
        t => t
            .DescribedAs("Unlists all prerelease packages below the configured stable version.")
            .RequiresParam(nameof(NugetFeed), nameof(NugetApiKey), nameof(PrereleaseCleanupBelowVersion))
            .Executes(cancellationToken =>
                UnlistPrereleasesBelowVersionForPackages(
                    NugetFeed,
                    NugetApiKey,
                    ProjectsToPack,
                    SemVer.Parse(PrereleaseCleanupBelowVersion),
                    cancellationToken));
}
```

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

