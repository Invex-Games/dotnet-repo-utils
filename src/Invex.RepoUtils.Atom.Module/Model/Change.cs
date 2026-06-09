namespace Invex.RepoUtils.Atom.Module.Model;

/// <summary>
/// Represents the set of line-level changes detected for a single file when diffing two commits.
/// </summary>
/// <param name="Path">The absolute, rooted path of the file that was changed.</param>
/// <param name="AddedLines">The lines that were added to the file in the newer commit.</param>
/// <param name="DeletedLines">The lines that were removed from the file relative to the older commit.</param>
[PublicAPI]
public sealed record Change(RootedPath Path, List<Line> AddedLines, List<Line> DeletedLines);
