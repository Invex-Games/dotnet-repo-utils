# Invex.RepoUtils.TestUtils

A test utility library that makes it easy to snapshot-test the public API surface of your assemblies. It uses reflection to extract all public types and their members, serialises the result to JSON, and pairs well with [Verify](https://github.com/VerifyTests/Verify) for approval-based testing.

## Overview

When combined with the [PublicApiAnalyzers](../public-api-analyzers/index.md) package, `TestUtils` provides a safety net that catches unintentional public API changes during code review. Any modification to your public surface will cause a snapshot diff, making breaking changes immediately visible in pull requests.

## Installation

```shell
dotnet add package Invex.RepoUtils.TestUtils
```

The package targets `net10.0` and is intended for use in test projects.

## Dependencies

- [JetBrains.Annotations](https://www.nuget.org/packages/JetBrains.Annotations) — for `[PublicAPI]` attribute
- [Verify.NUnit](https://www.nuget.org/packages/Verify.NUnit) — for snapshot testing integration

## Usage

### Basic API Surface Snapshot

Call `PublicApiSurfaceTestUtil.GetPublicApiSurface` with the assembly you want to inspect. The returned JSON string can be verified with your preferred snapshot testing library:

```csharp
using Invex.RepoUtils.TestUtils;

[TestFixture]
public class ApiSurfaceTests
{
    [Test]
    public Task PublicApiSurface()
    {
        var surface = PublicApiSurfaceTestUtil.GetPublicApiSurface(typeof(MyLibraryType).Assembly);
        return Verify(surface);
    }
}
```

### What Gets Captured

The utility inspects the target assembly and extracts:

| Member Type | Captured Information |
|-------------|---------------------|
| **Types** | Fully-qualified name |
| **Properties** | Name, property type |
| **Fields** | Name, field type |
| **Methods** | Name, return type, parameters (name + type) |

The following are **excluded**:

- Non-public types and members
- Special-name methods (property accessors, event accessors, operators)
- Compiler-generated members

### Output Format

The output is a JSON array of types, each containing its public members:

```json
[
  {
    "Name": "MyNamespace.MyClass",
    "Members": [
      { "Name": "Value", "Type": "System.String" },
      { "Name": "Execute", "ReturnType": "System.Void", "Parameters": [] }
    ]
  }
]
```

### Workflow Integration

The recommended workflow is:

1. **Write a test** that snapshots your public API surface.
2. **Commit the `.verified.txt` file** alongside your test code.
3. **On PR**, any changes to the public API will cause the snapshot to differ.
4. **Review the diff** — if the change is intentional, accept the new snapshot.
5. **The [breaking change check](../atom-module/breaking-changes.md)** uses these snapshot files to determine if a version bump is required.

### Example Test Project Setup

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Invex.RepoUtils.TestUtils" Version="*" />
    <PackageReference Include="NUnit" Version="*" />
    <PackageReference Include="Verify.NUnit" Version="*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MyLibrary\MyLibrary.csproj" />
  </ItemGroup>
</Project>
```

