using Type = Invex.RepoUtils.TestUtils.Model.Type;

namespace Invex.RepoUtils.TestUtils;

[PublicAPI]
public sealed class PublicApiSurfaceTestUtil
{
    public static string GetPublicApiSurface(Assembly assembly)
    {
        // Get all types in Invex.Atom assembly that are annotated with [PublicAPI] attribute
        var publicApiSurface = assembly
            .GetTypes()
            .Where(static t => t is { IsPublic: true })
            .Select(static t => new Type(t.FullName!,
                t
                    .GetMembers(BindingFlags.Instance |
                                BindingFlags.Static |
                                BindingFlags.Public |
                                BindingFlags.DeclaredOnly)
                    .Select(static m => GetMember(m))
                    .Where(static m => m is not null)
                    .Select(x => x!)
                    .OrderBy(static m => m.Name)
                    .ToList()))
            .OrderBy(static t => t.Name)
            .ToList();

        return JsonSerializer.Serialize(publicApiSurface);
    }

    private static IMember? GetMember(MemberInfo arg) =>
        arg switch
        {
            FieldInfo fieldInfo => new Field(fieldInfo.Name, fieldInfo.FieldType.FullName!),
            PropertyInfo propertyInfo => new Property(propertyInfo.Name, propertyInfo.PropertyType.FullName!),
            MethodInfo { IsSpecialName: false } methodInfo => new Method(methodInfo.Name,
                methodInfo.ReturnType.FullName!,
                methodInfo
                    .GetParameters()
                    .Select(p => new MethodParameter(p.Name!, p.ParameterType.FullName!))
                    .ToList()),
            _ => null,
        };
}
