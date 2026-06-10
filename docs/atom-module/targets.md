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

## IWaitForCopilotReview

Blocks until GitHub Copilot has finished reviewing a pull request. This fills the gap where GitHub cannot natively wait for a Copilot review before completing an auto-merge, allowing the target to be sequenced ahead of an auto-merge step.

### Interface

```csharp
public interface IWaitForCopilotReview : ICopilotReviewHelper, IGithubHelper, IGithubPrHelper
```

### Target

| Name | Required Parameters | Description |
|------|-------------------|-------------|
| `WaitForCopilotReview` | `GithubPullRequestNumber` | Waits until Copilot has finished reviewing the target pull request. |

### Behaviour

1. Polls the pull request's review state via GitHub's GraphQL API.
2. **Succeeds immediately** when Copilot was not requested as a reviewer (nothing to wait for).
3. Considers the review complete once Copilot is no longer a pending review request and a Copilot review exists.
4. **Fails** when Copilot does not finish reviewing within the configured timeout.

### Configuration

| Member | Type | Description |
|--------|------|-------------|
| `CopilotReviewerLogin` | `string` | The login of the Copilot reviewer bot to wait for. Defaults to `"Copilot"`. |
| `CopilotReviewTimeoutSeconds` | `int` | Maximum time, in seconds, to wait before failing. Defaults to `600`. |
| `CopilotReviewPollIntervalSeconds` | `int` | Delay, in seconds, between polls. Defaults to `15`. |
| `CopilotReviewToken` | `string?` | Optional PAT used to read the review state; falls back to `GithubToken`. Defined as a secret. |

### Workflow Example

```csharp
new(nameof(WaitForCopilotReview))
{
    Options =
    [
        BuildOptions.Target.SuppressArtifactPublishing,
        BuildOptions.Inject.Secret(nameof(GithubToken)),
        BuildOptions.Github.TokenPermissions.Set(new Permissions.Exact(new()
        {
            PullRequests = PermissionsLevel.Read,
        })),
        BuildOptions.Inject.Github.PullRequestNumber,
    ],
}
```

Sequence this target ahead of an auto-merge step (e.g. `ApproveDependabotPr`) so the merge only proceeds once Copilot's review has landed.





