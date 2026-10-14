using Type = Invex.RepoUtils.TestUtils.Model.Type;

namespace Invex.RepoUtils.TestUtils;

/// <summary>
///     A test helper that captures the public API surface of an assembly as JSON, for use in snapshot tests that
///     detect unintended public API changes.
/// </summary>
[PublicAPI]
public sealed class PublicApiSurfaceTestUtil
{
    /// <summary>
    ///     Gets a JSON description of the public types declared in <paramref name="assembly" /> and their public
    ///     members.
    /// </summary>
    /// <param name="assembly">The assembly whose public API surface to capture.</param>
    /// <returns>
    ///     A JSON array with one entry per top-level public type, ordered by full name, each listing the names of its
    ///     declared public instance and static fields, properties and methods (excluding constructors, accessors and
    ///     other special-name methods), ordered by name.
    /// </returns>
    /// <remarks>
    ///     Nested types are not included. Members are recorded by name only, so overloads appear once per overload and
    ///     signature changes that keep the same name are not detected.
    /// </remarks>
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
