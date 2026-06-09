namespace Invex.RepoUtils.TestUtils.Model;

internal sealed record Method(string Name, string ReturnType, IReadOnlyList<MethodParameter> Parameters) : IMember
{
    public override string ToString() =>
        $"{ReturnType} {Name}({string.Join(", ", Parameters)})";
}