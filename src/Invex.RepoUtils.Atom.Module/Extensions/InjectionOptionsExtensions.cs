namespace Invex.RepoUtils.Atom.Module.Extensions;

/// <summary>
/// Provides additional, GitHub-specific value injection options for build targets, extending the
/// Atom framework's built-in injection options.
/// </summary>
[PublicAPI]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
[SuppressMessage("ReSharper", "UnusedParameter.Global")]
public static class InjectionOptionsExtensions
{
    /// <summary>
    /// Groups the GitHub-specific injection options that can be applied to a workflow target.
    /// </summary>
    [PublicAPI]
    public sealed class GithubInjectionOptions
    {
        /// <summary>
        /// The lazily-created shared instance used to expose the GitHub injection options.
        /// </summary>
        internal static GithubInjectionOptions Instance => field ??= new();

        /// <summary>
        /// Injects the pull request number (read from the GitHub event payload's <c>number</c> field)
        /// as the <c>PullRequestNumber</c> parameter for the target.
        /// </summary>
        public IBuildOption PullRequestNumber =>
            field ??= BuildOptions.Inject.Param(nameof(IGithubPrHelper.GithubPullRequestNumber),
                TextExpressions.Github.GithubEvent["number"]);

        public IBuildOption DependabotEnableAutoMergePat =>
            field ??= BuildOptions.Inject.Secret(nameof(IApproveDependabotPr.DependabotEnableAutoMergePat));
    }

    extension(WorkflowBuildOptionsExtensions.InjectionBuildOptions _)
    {
        /// <summary>
        /// Entry point for the GitHub-specific injection options, accessed via
        /// <c>BuildOptions.Inject.Github</c>.
        /// </summary>
        [PublicAPI]
        public GithubInjectionOptions Github => GithubInjectionOptions.Instance;
    }
}
