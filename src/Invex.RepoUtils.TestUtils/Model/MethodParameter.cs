namespace Invex.RepoUtils.TestUtils.Model;

internal sealed record MethodParameter(string Name, string Type)
{
    public override string ToString() =>
        $"{Type} {Name}";
}