namespace Invex.RepoUtils.Atom.Module.Helpers;

/// <summary>
/// Exposes the GitHub pull request context that targets operating on a specific pull request require.
/// </summary>
[PublicAPI]
public interface IGithubPrHelper : IBuildAccessor
{
    /// <summary>
    /// The number of the pull request being operated on. Typically injected from the GitHub Actions
    /// event payload when running inside a workflow.
    /// </summary>
    [ParamDefinition("github-pull-request-number", "The pull request number to approve.")]
    int GithubPullRequestNumber => GetParam(() => GithubPullRequestNumber);
}
