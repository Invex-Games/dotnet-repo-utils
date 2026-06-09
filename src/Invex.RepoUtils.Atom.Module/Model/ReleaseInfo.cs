namespace Invex.RepoUtils.Atom.Module.Model;

/// <summary>
/// Captures the identifying information of a previously published release that is used as the
/// baseline for breaking change comparisons.
/// </summary>
/// <param name="CommitHash">The SHA of the commit that the release tag points at.</param>
/// <param name="Version">The semantic version associated with the release.</param>
[PublicAPI]
public sealed record ReleaseInfo(string CommitHash, SemVer Version);
