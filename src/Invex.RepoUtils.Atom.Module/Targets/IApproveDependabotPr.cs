namespace Invex.RepoUtils.Atom.Module.Targets;

/// <summary>
/// Provides a build target that automatically enables auto-merge on pull requests opened by
/// Dependabot, allowing dependency update PRs to merge once their required checks pass.
/// </summary>
[PublicAPI]
public interface IApproveDependabotPr : IGithubHelper, IGithubPrHelper
{
    /// <summary>
    /// The GitHub actor name used by Dependabot. Only pull requests opened by this actor are eligible
    /// for auto-merge.
    /// </summary>
    const string DependabotActorName = "dependabot[bot]";

    /// <summary>
    /// A GitHub personal access token with the permissions required to enable auto-merge on pull
    /// requests. A dedicated PAT is required because the default workflow token cannot trigger the
    /// downstream workflows needed to complete an auto-merge.
    /// </summary>
    [SecretDefinition("dependabot-enable-auto-merge-pat",
        "A GitHub PAT with permissions to enable auto-merge on pull requests.")]
    string? DependabotEnableAutoMergePat => GetParam(() => DependabotEnableAutoMergePat);

    /// <summary>
    /// Enables auto-merge on the target Dependabot pull request. The target validates that the pull
    /// request was opened by Dependabot before resolving the pull request and issuing the GraphQL
    /// mutation to enable auto-merge using the merge strategy.
    /// </summary>
    Target ApproveDependabotPr =>
        t => t
            .RequiresParam(nameof(GithubPullRequestNumber), nameof(DependabotEnableAutoMergePat))
            .Executes(async cancellationToken =>
            {
                var actor = Github.Variables.Actor;
                var owner = Github.Variables.RepositoryOwner;

                // The repository variable is "owner/name"; keep only the repository name.
                var repo = Github.Variables
                    .Repository
                    .Split('/')
                    .Last();

                Logger.LogInformation("Github API action context: {Context}",
                    new
                    {
                        Actor = actor,
                        GithubPullRequestNumber,
                        Owner = owner,
                        Repo = repo,
                    });

                // Guard against enabling auto-merge on pull requests that did not originate from Dependabot.
                if (actor != DependabotActorName)
                    throw new StepFailedException(
                        $"Only pull requests from {DependabotActorName} can be auto-approved.");

                // Authenticate using the dedicated PAT rather than the default workflow token.
                var productHeader = new ProductHeaderValue("Atom");

                var connection = new Connection(productHeader,
                    new InMemoryCredentialStore(DependabotEnableAutoMergePat));

                // Resolve the pull request node id required to target the auto-merge mutation.
                var prQuery = new Query()
                    .Repository(repo, owner)
                    .PullRequest(GithubPullRequestNumber)
                    .Select(p => new
                    {
                        p.Id,
                        p.HeadRefOid,
                    })
                    .Compile();

                var prQueryResult = await connection.Run(prQuery, cancellationToken: cancellationToken);

                if (prQueryResult.Id.Value is null)
                    throw new StepFailedException("Could not find pull request.");

                // Enable auto-merge using the standard merge commit strategy.
                var enableAutoMergeMutation = new Mutation()
                    .EnablePullRequestAutoMerge(new EnablePullRequestAutoMergeInput
                    {
                        PullRequestId = prQueryResult.Id,
                        MergeMethod = PullRequestMergeMethod.Merge,
                    })
                    .Select(x => new
                    {
                        x.ClientMutationId,
                    })
                    .Compile();

                var enableAutoMergeResult =
                    await connection.Run(enableAutoMergeMutation, cancellationToken: cancellationToken);

                if (enableAutoMergeResult is null)
                    throw new StepFailedException("Could not enable auto merge.");
            });
}
