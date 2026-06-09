namespace Invex.RepoUtils.Atom.Module.Targets;

/// <summary>
/// Provides a build target that checks a pull request for breaking changes to the public API surface
/// and reports the result back to GitHub as a check run.
/// </summary>
[PublicAPI]
public interface ICheckPrForBreakingChanges : IPrBreakingChangeHelper, ISetupBuildInfo, IGithubHelper, IGithubPrHelper
{
    /// <summary>
    /// The set of API definition files whose changes should be analyzed for breaking changes.
    /// Implementers should override this to point at their public API surface files; defaults to an
    /// empty set, which effectively disables the check.
    /// </summary>
    IEnumerable<RootedPath> BreakingChangeFilesToCheck => [];

    /// <summary>
    /// Runs the breaking change analysis for the current pull request using the build version produced
    /// by <see cref="ISetupBuildInfo"/> and publishes the resulting GitHub check run.
    /// </summary>
    Target CheckPrForBreakingChanges =>
        t => t
            .RequiresParam(nameof(GithubToken), nameof(GithubPullRequestNumber))
            .ConsumesVariable(nameof(SetupBuildInfo), nameof(BuildVersion))
            .Executes(async cancellationToken => await PerformPrBreakingChangeCheck(BuildVersion,
                GithubPullRequestNumber,
                BreakingChangeFilesToCheck,
                GithubToken,
                cancellationToken));
}
