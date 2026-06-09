# Configuration

The Public API Analyzers package is configurable through `.editorconfig` entries, allowing you to customize which attributes satisfy the rule and adjust severity levels.

## Valid Attributes

By default the analyzer accepts `PublicAPI` / `PublicAPIAttribute`. You can extend the set of attributes that satisfy the rule by adding a comma-separated list of attribute names. The `Attribute` suffix is optional — both forms are accepted:

```ini
# .editorconfig
[*.cs]
dotnet_code_quality.DecSm_Analyzers_ValidPublicApiAttributes = Experimental, MyCompanyApi
```

With this configuration, the following attributes would all satisfy the rule:

- `[PublicAPI]` / `[PublicAPIAttribute]` (always valid)
- `[Experimental]` / `[ExperimentalAttribute]`
- `[MyCompanyApi]` / `[MyCompanyApiAttribute]`

### Example

```csharp
// With the configuration above, this is valid:
[Experimental]
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

