# Dependabot Auto-Merge

The Atom module provides a target that automatically enables auto-merge on pull requests opened by Dependabot, allowing dependency updates to merge once their required checks pass.

## Overview

When Dependabot opens a pull request to update a dependency, you often want it to merge automatically once CI passes. The `ApproveDependabotPr` target handles this by:

1. Verifying the PR was opened by `dependabot[bot]`
2. Authenticating with a dedicated Personal Access Token (PAT)
3. Enabling auto-merge via GitHub's GraphQL API

## Why a Dedicated PAT?

GitHub's default `GITHUB_TOKEN` cannot trigger downstream workflows. When auto-merge completes and the PR is merged, you typically want other workflows (like your Build workflow) to fire. A dedicated PAT ensures those downstream workflows run.

## Setup

### 1. Create a Personal Access Token

Create a [fine-grained PAT](https://github.com/settings/tokens?type=beta) with the following permissions on your repository:

| Permission | Level | Purpose |
|-----------|-------|---------|
| Pull requests | Write | Enable auto-merge on PRs |
| Contents | Write | Allow the merge to proceed |

### 2. Add the Secret

Add the PAT as a repository secret named `dependabot-enable-auto-merge-pat` (or whatever name you configure).

### 3. Configure Your Build

Add `IApproveDependabotPr` to your build definition:

```csharp
[BuildDefinition]
[GenerateEntryPoint]
internal interface IBuild :
    IWorkflowBuildDefinition,
    IApproveDependabotPr
{
    // ...
}
```

### 4. Define the Workflow

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
                // Inject the PR number from the GitHub event payload
                BuildOptions.Inject.Github.PullRequestNumber,
                // Inject the dedicated PAT secret
                BuildOptions.Inject.Github.DependabotEnableAutoMergePat,
                // Only run when the actor is dependabot[bot]
                BuildOptions.Target.RunIfWorkflowCondition(
                    TextExpressions.Github.GithubActor.EqualToString("dependabot[bot]")),
            ],
        },
    ],
    Types = [WorkflowTypes.Github.Action],
}
```

## Combining with Dependabot Configuration

You can also use the Atom Dependabot workflow preset to generate your `dependabot.yml`:

```csharp
WorkflowPresets.Github.Dependabot(new()
{
    Registries = new Dictionary<string, DependabotRegistry>
    {
        ["nuget"] = new()
        {
            Type = RegistryType.NugetFeed,
            Url = WorkflowLabels.Github.Dependabot.NugetUrl,
        },
    },
    Updates =
    [
        new()
        {
            Directory = "/",
            PackageEcosystem = WorkflowLabels.Github.Dependabot.NugetEcosystem,
            Registries = new DependabotRegistries.Named("nuget"),
            Groups = new Dictionary<string, DependabotGroup>
            {
                ["nuget-deps"] = new DependabotGroup.FromPatterns
                {
                    Patterns = ["*"],
                },
            },
            Schedule = new()
            {
                Interval = ScheduleInterval.Daily,
            },
            TargetBranch = "main",
            OpenPullRequestsLimit = 10,
        },
    ],
})
```

This generates a Dependabot configuration that groups all NuGet dependency updates together and targets the `main` branch.

## Security Considerations

- The PAT should have the **minimum required permissions**.
- The target **only** enables auto-merge when the actor is `dependabot[bot]` — it will throw a `StepFailedException` for any other actor.
- Consider using a [GitHub App](https://docs.github.com/en/apps) instead of a PAT for production environments to avoid tying secrets to individual user accounts.

