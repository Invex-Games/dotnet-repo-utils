# API Reference

This reference covers the public types of the `Invex.RepoUtils.TestUtils` and `Invex.RepoUtils.Atom.Module`
packages, generated from their XML documentation comments.

> [!NOTE]
> `Invex.RepoUtils.PublicApiAnalyzers` is a Roslyn analyzer with no consumable API, so it is not listed here.
> Its diagnostics are documented in [Rules](../docs/public-api-analyzers/rules.md).

## Invex.RepoUtils.TestUtils

### Invex.RepoUtils.TestUtils

| Type | Kind | Description |
|------|------|-------------|
| [`PublicApiSurfaceTestUtil`](xref:Invex.RepoUtils.TestUtils.PublicApiSurfaceTestUtil) | Class | Serialises the public types and member names of an assembly to JSON for snapshot testing. |

## Invex.RepoUtils.Atom.Module

### Invex.RepoUtils.Atom.Module.Targets

| Type | Kind | Description |
|------|------|-------------|
| [`ICheckPrForBreakingChanges`](xref:Invex.RepoUtils.Atom.Module.Targets.ICheckPrForBreakingChanges) | Interface | Target that checks a pull request for public API breaking changes and reports the result as a GitHub check run. |
| [`IApproveDependabotPr`](xref:Invex.RepoUtils.Atom.Module.Targets.IApproveDependabotPr) | Interface | Target that enables auto-merge on pull requests opened by Dependabot. |
| [`IWaitForCopilotReview`](xref:Invex.RepoUtils.Atom.Module.Targets.IWaitForCopilotReview) | Interface | Target that blocks until GitHub Copilot has finished reviewing a pull request. |

### Invex.RepoUtils.Atom.Module.Helpers

| Type | Kind | Description |
|------|------|-------------|
| [`IApiSurfaceHelper`](xref:Invex.RepoUtils.Atom.Module.Helpers.IApiSurfaceHelper) | Interface | Diffs API definition files between two commits and classifies changes as major (removals) or minor (additions). |
| [`IPrBreakingChangeHelper`](xref:Invex.RepoUtils.Atom.Module.Helpers.IPrBreakingChangeHelper) | Interface | Runs the end-to-end breaking change check for a pull request against the latest release baseline. |
| [`IGithubPrHelper`](xref:Invex.RepoUtils.Atom.Module.Helpers.IGithubPrHelper) | Interface | Exposes the number of the pull request a target operates on. |
| [`ICopilotReviewHelper`](xref:Invex.RepoUtils.Atom.Module.Helpers.ICopilotReviewHelper) | Interface | Polls a pull request until GitHub Copilot's review has landed or a timeout elapses. |
| [`IDocFxHelper`](xref:Invex.RepoUtils.Atom.Module.Helpers.IDocFxHelper) | Interface | Builds, serves and publishes DocFX documentation to the `gh-pages` branch. |
| [`INugetPackageUnlistHelper`](xref:Invex.RepoUtils.Atom.Module.Helpers.INugetPackageUnlistHelper) | Interface | Discovers and unlists superseded or obsolete prerelease packages on a NuGet feed. |

### Invex.RepoUtils.Atom.Module.Model

| Type | Kind | Description |
|------|------|-------------|
| [`BreakingChanges`](xref:Invex.RepoUtils.Atom.Module.Model.BreakingChanges) | Record | Major and minor changes found when comparing two versions of the API surface. |
| [`Change`](xref:Invex.RepoUtils.Atom.Module.Model.Change) | Record | Lines added and deleted in a single file between two commits. |
| [`ReleaseInfo`](xref:Invex.RepoUtils.Atom.Module.Model.ReleaseInfo) | Record | Commit and version of the release used as the breaking-change baseline. |
| [`UnlistResult`](xref:Invex.RepoUtils.Atom.Module.Model.UnlistResult) | Record | Result of attempting to unlist one package version. |
| [`UnlistOutcome`](xref:Invex.RepoUtils.Atom.Module.Model.UnlistOutcome) | Enum | Whether a package version was unlisted, skipped or failed. |

### Invex.RepoUtils.Atom.Module.Extensions

| Type | Kind | Description |
|------|------|-------------|
| [`InjectionOptionsExtensions`](xref:Invex.RepoUtils.Atom.Module.Extensions.InjectionOptionsExtensions) | Class | Adds `BuildOptions.Inject.Github` to Atom's injection options. |
| [`InjectionOptionsExtensions.GithubInjectionOptions`](xref:Invex.RepoUtils.Atom.Module.Extensions.InjectionOptionsExtensions.GithubInjectionOptions) | Class | GitHub injection options: `PullRequestNumber` and `DependabotEnableAutoMergePat`. |

## Typical flow

```csharp
using Invex.RepoUtils.Atom.Module.Extensions;
using Invex.RepoUtils.Atom.Module.Targets;

[BuildDefinition]
[GenerateEntryPoint]
internal interface IBuild : IWorkflowBuildDefinition, ICheckPrForBreakingChanges
{
    // Snapshots written by PublicApiSurfaceTestUtil in your test projects.
    IEnumerable<RootedPath> ICheckPrForBreakingChanges.BreakingChangeFilesToCheck =>
        RootedFileSystem
            .Directory
            .GetFiles(RootedFileSystem.AtomRootDirectory / "tests", "*.verified.txt", SearchOption.AllDirectories)
            .Select(RootedFileSystem.CreateRootedPath);

    IReadOnlyList<WorkflowDefinition> IWorkflowBuildDefinition.Workflows =>
    [
        new("Validate")
        {
            Triggers = [WorkflowTriggers.PullIntoMain],
            Targets =
            [
                new(nameof(SetupBuildInfo)),
                new(nameof(CheckPrForBreakingChanges))
                {
                    Options =
                    [
                        BuildOptions.Inject.Secret(nameof(GithubToken)),
                        BuildOptions.Github.TokenPermissions.Set(new Permissions.Exact(new()
                        {
                            IdTokens = PermissionsLevel.Write,
                            Contents = PermissionsLevel.Write,
                            PullRequests = PermissionsLevel.Write,
                            Checks = PermissionsLevel.Write,
                        })),
                        BuildOptions.Inject.Github.PullRequestNumber,
                    ],
                },
            ],
            Types = [WorkflowTypes.Github.Action],
        },
    ];
}
```

For concepts and walkthroughs, start with the [Introduction](../docs/introduction.md).
