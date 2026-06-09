# Targets

The Atom module provides the following build targets that can be mixed into your `IBuild` definition.

## IApproveDependabotPr

Automatically enables auto-merge on pull requests opened by `dependabot[bot]`.

### Interface

```csharp
public interface IApproveDependabotPr : IGithubHelper, IGithubPrHelper
```

### Target

| Name | Required Parameters | Description |
|------|-------------------|-------------|
| `ApproveDependabotPr` | `GithubPullRequestNumber`, `DependabotEnableAutoMergePat` | Enables auto-merge on the target Dependabot pull request. |

### Behaviour

1. Validates that the pull request was opened by `dependabot[bot]`.
2. Authenticates using a dedicated PAT (the default workflow token cannot trigger downstream workflows).
3. Resolves the pull request node ID via GitHub's GraphQL API.
4. Issues the `EnablePullRequestAutoMerge` mutation with the **Merge** strategy.

### Configuration

| Member | Type | Description |
|--------|------|-------------|
| `DependabotActorName` | `const string` | The GitHub actor name used by Dependabot (`"dependabot[bot]"`). |
| `DependabotEnableAutoMergePat` | `string?` | A GitHub PAT with permissions to enable auto-merge. Defined as a secret. |

### Workflow Example

```csharp
new("Dependabot Enable auto-merge")
{
    Triggers = [WorkflowTriggers.PullIntoMain],
    Targets =
    [
        new(nameof(ApproveDependabotPr))
        {
            Options =
            [
                BuildOptions.Inject.Github.PullRequestNumber,
                BuildOptions.Inject.Github.DependabotEnableAutoMergePat,
                BuildOptions.Target.RunIfWorkflowCondition(
                    TextExpressions.Github.GithubActor.EqualToString("dependabot[bot]")),
            ],
        },
    ],
    Types = [WorkflowTypes.Github.Action],
}
```

---

## ICheckPrForBreakingChanges

Detects public API breaking changes in a pull request and reports the result as a GitHub check run.

### Interface

```csharp
public interface ICheckPrForBreakingChanges : IPrBreakingChangeHelper, ISetupBuildInfo, IGithubHelper, IGithubPrHelper
```

### Target

| Name | Required Parameters | Consumed Variables | Description |
|------|-------------------|-------------------|-------------|
| `CheckPrForBreakingChanges` | `GithubToken`, `GithubPullRequestNumber` | `BuildVersion` (from `SetupBuildInfo`) | Runs the breaking change analysis and publishes a GitHub check run. |

### Configuration

| Member | Type | Description |
|--------|------|-------------|
| `BreakingChangeFilesToCheck` | `IEnumerable<RootedPath>` | The API definition files to analyze. Defaults to empty (check disabled). |

### Workflow Example

```csharp
new(nameof(CheckPrForBreakingChanges))
{
    Options =
    [
        BuildOptions.Target.SuppressArtifactPublishing,
        BuildOptions.Inject.Secret(nameof(GithubToken)),
        BuildOptions.Github.TokenPermissions.Set(new Permissions.Exact(new()
        {
            IdTokens = PermissionsLevel.Write,
            Contents = PermissionsLevel.Write,
            PullRequests = PermissionsLevel.Write,
            Checks = PermissionsLevel.Write,
        })),
        BuildOptions.Inject.Github.PullRequestNumber,
        BuildOptions.Target.RunIfWorkflowCondition(
            TextExpressions.Github.GithubEventName.EqualToString("pull_request")),
    ],
}
```

See [Breaking Change Detection](breaking-changes.md) for details on how the analysis works.

---

## IUnlistSupersededPrereleases

Unlists prerelease packages that have been superseded by a just-published version, and reports the
outcome to the Atom build report. Use it after a package-publish target to keep the feed tidy.

### Interface

```csharp
public interface IUnlistSupersededPrereleases : INugetPackageUnlistHelper, ISetupBuildInfo
```

### Target

| Name | Required Parameters | Consumed Variables | Description |
|------|-------------------|-------------------|-------------|
| `UnlistSupersededPrereleases` | `NugetUnlistFeed`, `NugetUnlistApiKey` | `BuildVersion` (from `SetupBuildInfo`) | Unlists prereleases superseded by the published version and reports the result. |

### Configuration

| Member | Type | Description |
|--------|------|-------------|
| `PackagesToUnlist` | `IEnumerable<string>` | The package ids whose superseded prereleases should be unlisted. Defaults to empty (target disabled). |
| `NugetUnlistFeed` | `string` | The service index URL of the feed. Parameter `nuget-unlist-feed`; defaults to `https://api.nuget.org/v3/index.json`. |
| `NugetUnlistApiKey` | `string` | The API key used to authenticate the unlist (`DELETE`) requests. Defined as the secret `nuget-unlist-api-key`. |
| `UnlistSupersededPrereleasesDependencies` | `IEnumerable<string>` | Names of targets this target depends on (for example, the package push target) so it runs after publishing. Defaults to none. |

### Behaviour

Given a published version, the target unlists every published prerelease of the **same** core
`MAJOR.MINOR.PATCH` with **lower** SemVer precedence:

- Publishing the stable `1.1.0` unlists `1.1.0-alpha.*`, `1.1.0-beta.*`, `1.1.0-rc.*`.
- Publishing the prerelease `1.1.0-rc.2` unlists `1.1.0-beta.*` and `1.1.0-rc.1`, but leaves
  `1.1.0-rc.2`, later prereleases, and the stable `1.1.0` untouched.
- Prereleases of a different core version (such as a `1.0.1-rc.1` patch line) and any higher version
  are never touched.

A summary table of every package/version processed and its outcome (`Unlisted`, `Skipped`, or
`Failed`) is written to the Atom build report.

### Workflow Example

```csharp
new(nameof(UnlistSupersededPrereleases))
{
    Options =
    [
        BuildOptions.Target.SuppressArtifactPublishing,
        BuildOptions.Inject.Secret(nameof(NugetUnlistApiKey)),
    ],
}
```

The target reads versions through the NuGet flat-container API and unlists via HTTP `DELETE`. See
[INugetPackageUnlistHelper](helpers.md#inugetpackageunlisthelper) for the underlying logic and retry
behaviour.


