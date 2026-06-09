namespace Invex.RepoUtils.TestUtils.Model;

internal sealed record Field(string Name, string Type) : IMember
{
    public override string ToString() =>
        $"{Type} {Name}";
}
