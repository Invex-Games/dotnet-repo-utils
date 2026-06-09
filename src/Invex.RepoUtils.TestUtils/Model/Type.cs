namespace Invex.RepoUtils.TestUtils.Model;

internal sealed record Type(string Name, [UsedImplicitly] IReadOnlyList<IMember> Members);
