namespace Invex.RepoUtils.Atom.Module.Helpers;

/// <summary>
/// Provides functionality for analyzing the public API surface of the repository by diffing the
/// tracked API definition files between two commits, and classifying the resulting changes as
/// either major (removals) or minor (additions) breaking changes.
/// </summary>
[PublicAPI]
public interface IApiSurfaceHelper : IBuildAccessor
{
    /// <summary>
    /// Compares the contents of the supplied files between two commits and determines which changes
    /// constitute breaking changes to the public API surface.
    /// </summary>
    /// <param name="oldVersion">The semantic version associated with <paramref name="oldCommitHash"/> (used for logging).</param>
    /// <param name="oldCommitHash">The SHA of the baseline commit to compare from.</param>
    /// <param name="newVersion">The semantic version associated with <paramref name="newCommitHash"/> (used for logging).</param>
    /// <param name="newCommitHash">The SHA of the commit to compare to.</param>
    /// <param name="filesToCheck">The API definition files whose changes should be analyzed.</param>
    /// <returns>
    /// A <see cref="BreakingChanges"/> instance describing the major and minor breaking changes that
    /// were detected. When no relevant changes exist, the returned lists are empty.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when either <paramref name="oldCommitHash"/> or <paramref name="newCommitHash"/> cannot
    /// be resolved to a commit in the repository.
    /// </exception>
    BreakingChanges IdentifyBreakingChanges(
        SemVer oldVersion,
        string oldCommitHash,
        SemVer newVersion,
        string newCommitHash,
        params IEnumerable<RootedPath> filesToCheck)
    {
        var filesToCheckArray = filesToCheck.ToArray();

        var filesToCheckDisplay = string.Join(", ", filesToCheckArray.Select(f => f.ToString()));

        Logger.LogDebug("Identifying breaking changes with options: {@Options}",
            new
            {
                oldVersion,
                oldCommitHash,
                newVersion,
                newCommitHash,
                filesToCheck = filesToCheckDisplay,
            });

        var targetFiles = FormatTargetFiles(filesToCheckArray);

        // Open the repository at the Atom root and resolve both commits up front so we can fail fast
        // if either side of the comparison is missing.
        using var repo = new Repository(RootedFileSystem.AtomRootDirectory);
        var oldCommit = repo.Lookup<Commit>(oldCommitHash);

        if (oldCommit?.IsMissing is not false)
            throw new InvalidOperationException($"Commit {oldCommitHash} is missing.");

        var newCommit = repo.Lookup<Commit>(newCommitHash);

        if (newCommit?.IsMissing is not false)
            throw new InvalidOperationException($"Commit {newCommitHash} is missing.");

        // Produce a patch describing how the trees of the two commits differ.
        var changes = repo.Diff.Compare<Patch>(oldCommit.Tree, newCommit.Tree);

        // Nothing changed between the two commits, so there can be no breaking changes. This guard is
        // evaluated before the logging below so we never dereference a null patch.
        if (changes is null or { LinesAdded: 0, LinesDeleted: 0 })
            return new([], []);

        Logger.LogDebug("Changes: {@Changes}",
            new
            {
                changes.Content,
                changes.LinesDeleted,
                changes.LinesAdded,
            });

        // Restrict the diff to the API definition files we care about and only keep files where
        // lines were removed since those are the candidates for breaking changes.
        IReadOnlyList<Change> suspiciousChanges = changes
            .Where(x => targetFiles.Contains(x.Path) && x.LinesDeleted > 0)
            .Select(x => new Change(RootedFileSystem.AtomRootDirectory / x.Path, x.AddedLines, x.DeletedLines))
            .ToList();

        Logger.LogDebug("Suspicious changes: {@SuspiciousChanges}", suspiciousChanges);

        // A change is treated as a major (i.e. removal) breaking change when one or more lines were
        // deleted and none of those deletions are merely trailing/leading comma reformatting. A line
        // that only adds or removes a comma typically reflects list reordering rather than the removal
        // of an actual API member.
        var majorChanges = suspiciousChanges
            .Where(x => x.DeletedLines.Count > 0 &&
                        x
                            .DeletedLines
                            .Select(l => l.Content.Trim())
                            .All(deletedLine => !deletedLine.StartsWith(',') && !deletedLine.EndsWith(',')))
            .ToList();

        Logger.LogDebug("Major changes: {@MajorChanges}", majorChanges);

        // Anything suspicious that is not a major change but still adds lines is considered a minor
        // (i.e. additive) breaking change.
        var minorChanges = suspiciousChanges
            .Except(majorChanges)
            .Where(x => x.AddedLines.Count > 0)
            .ToList();

        Logger.LogDebug("Minor changes: {@MinorChanges}", minorChanges);

        return new(majorChanges, minorChanges);
    }

    /// <summary>
    /// Normalizes the supplied paths into repository-relative, forward-slash separated paths so they
    /// can be matched against the paths reported by the Git diff.
    /// </summary>
    /// <param name="filesToCheck">The paths to normalize.</param>
    /// <returns>A set of normalized, repository-relative file paths.</returns>
    private HashSet<string> FormatTargetFiles(IEnumerable<RootedPath> filesToCheck)
    {
        var targetFiles = filesToCheck
            // Make every path relative to the repository root; absolute paths are converted, while
            // already-relative paths are left as-is.
            .Select(x => RootedFileSystem.Path.IsPathRooted(x)
                ? RootedFileSystem.Path.GetRelativePath(RootedFileSystem.AtomRootDirectory, x)
                : x)
            // Git always reports paths with forward slashes, so normalize separators to match.
            .Select(x => x.Replace("\\", "/"))
            // Strip any leading slash so the comparison is purely repository-relative.
            .Select(x => x.StartsWith('/')
                ? x[1..]
                : x)
            .ToHashSet();

        return targetFiles;
    }
}
