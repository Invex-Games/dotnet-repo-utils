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

