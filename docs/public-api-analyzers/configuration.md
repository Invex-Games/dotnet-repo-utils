# Configuration

The Public API Analyzers package is configurable through `.editorconfig` entries. Use this page when the default `[PublicAPI]` attribute is not the only marker you want to accept, or when you need a different severity for some files or projects.

## Valid Attributes

By default the analyzer accepts `PublicAPI` / `PublicAPIAttribute`. You can extend the set of attributes that satisfy the rule by adding a comma-separated list of attribute names. The `Attribute` suffix is optional — both forms are accepted:

```ini
# .editorconfig
[*.cs]
dotnet_code_quality.Invex_RepoUtils_PublicApiAnalyzers_ValidPublicApiAttributes = Experimental, MyCompanyApi
```

With this configuration, the following attributes would all satisfy the rule:

- `[PublicAPI]` / `[PublicAPIAttribute]` (always valid)
- `[Experimental(...)]` / `[ExperimentalAttribute(...)]`
- `[MyCompanyApi]` / `[MyCompanyApiAttribute]`

### Example

```csharp
using System.Diagnostics.CodeAnalysis;

// With the configuration above, this is valid:
[Experimental("MYLIB001")]
public class PreviewFeature { }

// And so is this:
[MyCompanyApi]
public class InternalApi { }
```

## Severity Configuration

To change the severity of the rule, use the standard `.editorconfig` diagnostic severity setting:

```ini
[*.cs]
dotnet_diagnostic.IPAA0001.severity = error
```

Available severity levels:

| Level | Behavior |
|-------|----------|
| `error` | Fails the build |
| `warning` | Reports a warning (default) |
| `suggestion` | Shows as an IDE suggestion |
| `silent` | Hidden in IDE, still available in code fixes |
| `none` | Completely disabled |

## Scoping Configuration

You can scope the configuration to specific file patterns using EditorConfig's glob syntax:

```ini
# Only enforce in production source files
[src/**/*.cs]
dotnet_diagnostic.IPAA0001.severity = error

# Relax in test projects
[tests/**/*.cs]
dotnet_diagnostic.IPAA0001.severity = none
```

## Project-Level Configuration

To disable the analyzer for an entire project, you can add a `NoWarn` property in your `.csproj`:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);IPAA0001</NoWarn>
</PropertyGroup>
```

Or remove the analyzer from specific projects:

```xml
<ItemGroup>
  <PackageReference Include="Invex.RepoUtils.PublicApiAnalyzers" Version="*">
    <PrivateAssets>all</PrivateAssets>
    <ExcludeAssets>analyzers</ExcludeAssets>
  </PackageReference>
</ItemGroup>
```

## See also

- [Rules](rules.md)
- [Public API Analyzers](index.md)
