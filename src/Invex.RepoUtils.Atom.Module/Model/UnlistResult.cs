namespace Invex.RepoUtils.Atom.Module.Model;

/// <summary>
/// Describes the outcome of attempting to unlist a single package version from a NuGet feed.
/// </summary>
[PublicAPI]
public enum UnlistOutcome
{
    /// <summary>
    /// The package version was successfully unlisted.
    /// </summary>
    Unlisted,

    /// <summary>
    /// The package version did not need to be unlisted (for example, it was already removed or
    /// could not be found on the feed) and was therefore skipped without being treated as a failure.
    /// </summary>
    Skipped,

    /// <summary>
    /// The package version could not be unlisted because of an unexpected error.
    /// </summary>
    Failed,
}

/// <summary>
/// Captures the result of attempting to unlist a specific package version that was superseded by a
/// newly published version.
/// </summary>
/// <param name="PackageId">The id of the package whose version was processed.</param>
/// <param name="Version">The semantic version that was processed.</param>
/// <param name="Outcome">The outcome of the unlist attempt.</param>
/// <param name="Message">An optional human-readable detail explaining the outcome.</param>
[PublicAPI]
public sealed record UnlistResult(string PackageId, SemVer Version, UnlistOutcome Outcome, string? Message = null);

