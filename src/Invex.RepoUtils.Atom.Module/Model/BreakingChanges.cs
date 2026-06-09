namespace Invex.RepoUtils.Atom.Module.Model;

/// <summary>
/// Describes the breaking changes discovered when comparing the public API surface of two versions,
/// grouped by the severity of the version bump they require.
/// </summary>
/// <param name="MajorChanges">
/// Changes that remove or alter existing API members and therefore require a major version bump
/// (for example, a public member being deleted).
/// </param>
/// <param name="MinorChanges">
/// Changes that add to the API surface and therefore require a minor version bump
/// (for example, a new public member being introduced).
/// </param>
[PublicAPI]
public sealed record BreakingChanges(IReadOnlyList<Change> MajorChanges, IReadOnlyList<Change> MinorChanges);
