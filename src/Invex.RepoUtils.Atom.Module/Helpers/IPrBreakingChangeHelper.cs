namespace Invex.RepoUtils.Atom.Module.Helpers;

/// <summary>
/// Orchestrates the end-to-end breaking change check for a pull request: it locates the most recent
/// release to use as a baseline, identifies breaking changes against it, validates that the version
/// has been bumped appropriately, and reports the result back to GitHub as a check run.
/// </summary>
[PublicAPI]
public interface IPrBreakingChangeHelper : IApiSurfaceHelper
{
    /// <summary>
    /// Runs the complete breaking change analysis for a pull request and publishes a GitHub check run
    /// summarizing the outcome.
    /// </summary>
    /// <param name="currentVersion">The semantic version produced for the current pull request.</param>
    /// <param name="pullRequestNumber">The number of the pull request being checked.</param>
    /// <param name="filesToCheck">The API definition files whose changes should be analyzed.</param>
    /// <param name="githubToken">The GitHub token used to authenticate the check run creation.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <remarks>
    /// When no previous release can be found, the check is skipped as there is no baseline to compare
    /// against. Otherwise a check run is created with a <c>success</c> conclusion when the version has
    /// been bumped to cover the detected changes, or <c>failure</c> when a required bump is missing.
    /// </remarks>
    async Task PerformPrBreakingChangeCheck(
        SemVer currentVersion,
        int pullRequestNumber,
        IEnumerable<RootedPath> filesToCheck,
        string githubToken,
        CancellationToken cancellationToken)
    {
        var owner = Github.Variables.RepositoryOwner;
        Logger.LogDebug("Target repository owner: {Owner}", owner);

        using var repo = new Repository(RootedFileSystem.AtomRootDirectory);

        // The tip of the current branch represents the state being proposed by the pull request.
        var currentCommitHash = repo.Head.Tip.Sha;
        Logger.LogDebug("Current commit hash: {CommitHash}", currentCommitHash);

        Logger.LogDebug("Current version: {Version}", currentVersion);

        // Find the most recent release prior to the current version to use as the comparison baseline.
        var latestReleaseInfo = FindLatestReleaseInfo(repo, currentVersion);
        Logger.LogDebug("Latest release info: {ReleaseInfo}", latestReleaseInfo);

        if (latestReleaseInfo is null)
        {
            Logger.LogInformation("No previous release found. Skipping breaking changes check.");

            return;
        }

        Logger.LogInformation(
            "Comparing current version {CurrentVersion} with latest release version {LatestVersion} to identify breaking changes.",
            currentVersion,
            latestReleaseInfo.Version);

        // Diff the current commit against the baseline release to classify the changes.
        var breakingChanges = IdentifyBreakingChanges(latestReleaseInfo.Version,
            latestReleaseInfo.CommitHash,
            currentVersion,
            currentCommitHash,
            filesToCheck);

        Logger.LogInformation("Identified {MajorCount} major breaking changes and {MinorCount} minor breaking changes.",
            breakingChanges.MajorChanges.Count,
            breakingChanges.MinorChanges.Count);

        // Build the markdown summary shown on the check run. The message varies depending on whether
        // major or minor changes were found, and whether the corresponding version component has
        // already been incremented.
        var body = breakingChanges.MajorChanges.Count > 0
            ? currentVersion.Major > latestReleaseInfo.Version.Major
                ? $"""
                   ℹ️ **Major Breaking Changes Detected**

                   This pull request contains major breaking changes to the public API surface.

                   **Version Bump Status:** ✅ Major version has been bumped from `{latestReleaseInfo.Version.Major}` to `{currentVersion.Major}`

                   **Files with breaking changes:**
                   {string.Join("\n", breakingChanges.MajorChanges.Select(x => $"- `{x.Path}`"))}

                   The major version has already been appropriately incremented to reflect these breaking changes.
                   """
                : $"""
                   ⚠️ **Major Breaking Changes Detected - Action Required**

                   This pull request contains major breaking changes to the public API surface, but the major version has not been bumped.

                   **Current Version:** `{currentVersion}`
                   **Latest Release:** `{latestReleaseInfo.Version}`

                   **Files with breaking changes:**
                   {string.Join("\n", breakingChanges.MajorChanges.Select(x => $"- `{x.Path}` ({x.DeletedLines.Count} lines removed)"))}

                   **Required Action:** Please increment the major version number before merging this pull request.
                   """
            : breakingChanges.MinorChanges.Count > 0
                // A minor breaking change is adequately covered by either a minor *or* a major version
                // bump, so both are treated as satisfying the requirement (keeping this in sync with the
                // hasInvalidChanges check below).
                ? currentVersion.Major > latestReleaseInfo.Version.Major ||
                  currentVersion.Minor > latestReleaseInfo.Version.Minor
                    ? $"""
                       ℹ️ **Minor Breaking Changes Detected**

                       This pull request contains minor breaking changes to the public API surface.

                       **Version Bump Status:** ✅ Minor version has been bumped from `{latestReleaseInfo.Version.Minor}` to `{currentVersion.Minor}`

                       **Files with breaking changes:**
                       {string.Join("\n", breakingChanges.MinorChanges.Select(x => $"- `{x.Path}`"))}

                       The minor version has already been appropriately incremented to reflect these changes.
                       """
                    : $"""
                       ⚠️ **Minor Breaking Changes Detected - Action Required**

                       This pull request contains minor breaking changes to the public API surface, but the minor version has not been bumped.

                       **Current Version:** `{currentVersion}`
                       **Latest Release:** `{latestReleaseInfo.Version}`

                       **Files with breaking changes:**
                       {string.Join("\n", breakingChanges.MinorChanges.Select(x => $"- `{x.Path}` ({x.AddedLines.Count} lines added)"))}

                       **Required Action:** Please increment the minor version number before merging this pull request.
                       """
                : """
                  ✅ **No Breaking Changes Detected**

                  This pull request does not contain any breaking changes to the public API surface.
                  Safe to merge without version bump considerations.
                  """;

        var hasInvalidChanges = breakingChanges switch
        {
            // Major changes require the major version to have been bumped.
            { MajorChanges.Count: > 0 } when currentVersion.Major <= latestReleaseInfo.Version.Major => true,
            // Minor changes require at least the minor version to have been bumped (a major bump also covers it).
            { MinorChanges.Count: > 0 } when currentVersion.Major <= latestReleaseInfo.Version.Major &&
                                             currentVersion.Minor <= latestReleaseInfo.Version.Minor => true,
            _ => false,
        };

        Logger.LogInformation("Adding check status to pull request with status: {Status}",
            hasInvalidChanges
                ? "failure"
                : "success");

        await AddCheckStatus(pullRequestNumber,
            owner,
            hasInvalidChanges
                ? "failure"
                : "success",
            body,
            githubToken,
            cancellationToken);
    }

    /// <summary>
    /// Finds the most recent release tag that represents a version older than the current version, to
    /// be used as the baseline for breaking change analysis.
    /// </summary>
    /// <param name="repo">The repository whose tags should be searched.</param>
    /// <param name="currentVersion">The current version; only releases older than this are considered.</param>
    /// <returns>
    /// The <see cref="ReleaseInfo"/> for the highest version that precedes <paramref name="currentVersion"/>,
    /// or <see langword="null"/> when no suitable release tag exists.
    /// </returns>
    ReleaseInfo? FindLatestReleaseInfo(Repository repo, SemVer currentVersion)
    {
        // Parse every tag of the form "v{semver}" into a version, discarding tags that do not follow
        // the convention or that are not strictly older than the current version.
        var releaseVersions = repo
            .Tags
            .Select(x => new
            {
                Tag = x,
                Version = !x.FriendlyName.StartsWith('v')
                    ? null
                    : !SemVer.TryParse(x.FriendlyName[1..], out var version)
                        ? null
                        : version,
            })
            .Where(x => x.Version is not null && x.Version < currentVersion)
            .Select(x => new
            {
                Tag = x.Tag!,
                Version = x.Version!,
            })
            .ToList();

        if (releaseVersions.Count is 0)
        {
            Logger.LogWarning("No release found for current version {CurrentVersion}.", currentVersion);

            return null;
        }

        // The baseline is the newest of the eligible (older) releases.
        var version = releaseVersions.MaxBy(x => x.Version)!;

        return new(version.Tag.Target.Sha, version.Version);
    }

    /// <summary>
    /// Creates a GitHub check run on the pull request's head commit reporting the outcome of the
    /// breaking change analysis.
    /// </summary>
    /// <param name="pullRequestNumber">The number of the pull request to attach the check to.</param>
    /// <param name="owner">The owner (user or organization) of the target repository.</param>
    /// <param name="status">The conclusion of the check; <c>"success"</c> maps to a passing check, anything else fails.</param>
    /// <param name="description">The markdown summary displayed in the check run output.</param>
    /// <param name="githubToken">The GitHub token used to authenticate the GraphQL requests.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <exception cref="StepFailedException">
    /// Thrown when the repository, the pull request, or the resulting check run cannot be resolved/created.
    /// </exception>
    private async Task AddCheckStatus(
        int pullRequestNumber,
        string owner,
        string status,
        string description,
        string githubToken,
        CancellationToken cancellationToken)
    {
        // The repository variable is in "owner/name" form; we only need the repository name here.
        var repository = Github
            .Variables
            .Repository
            .Split('/')
            .Last();

        Logger.LogDebug("Target repository: {Repository}", repository);

        // Establish an authenticated GraphQL connection to the GitHub API.
        var productHeader = new ProductHeaderValue("Atom");
        var connection = new Connection(productHeader, new InMemoryCredentialStore(githubToken));

        // Resolve the repository's node id, which is required to create a check run.
        var repoQuery = new Query()
            .Repository(repository, owner)
            .Select(r => new
            {
                r.Id,
            })
            .Compile();

        var repoQueryResult = await connection.Run(repoQuery, cancellationToken: cancellationToken);

        if (repoQueryResult.Id.Value is null)
            throw new StepFailedException("Could not find repository.");

        // Resolve the pull request's node id and head commit SHA so the check run can be attached to it.
        var prQuery = new Query()
            .Repository(repository, owner)
            .PullRequest(pullRequestNumber)
            .Select(p => new
            {
                p.Id,
                p.HeadRefOid,
            })
            .Compile();

        var prQueryResult = await connection.Run(prQuery, cancellationToken: cancellationToken);

        if (prQueryResult.Id.Value is null)
            throw new StepFailedException("Could not find pull request.");


        // Create the completed check run with the appropriate conclusion and summary output.
        var checkRunMutation = new Mutation()
            .CreateCheckRun(new CreateCheckRunInput
            {
                RepositoryId = repoQueryResult.Id,
                Name = "API Surface Breaking Changes Check",
                HeadSha = prQueryResult.HeadRefOid,
                Status = RequestableCheckStatusState.Completed,
                Conclusion = status == "success"
                    ? CheckConclusionState.Success
                    : CheckConclusionState.Failure,
                CompletedAt = DateTimeOffset.UtcNow,
                Output = new()
                {
                    Title = "Breaking Changes Analysis",
                    Summary = description,
                },
            })
            .Select(x => new
            {
                x.ClientMutationId,
            })
            .Compile();

        var checkRunResult = await connection.Run(checkRunMutation, cancellationToken: cancellationToken);

        if (checkRunResult is null)
            throw new StepFailedException("Could not create check run.");
    }
}
