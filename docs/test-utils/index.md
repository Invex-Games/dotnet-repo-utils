# Test Utilities

`Invex.RepoUtils.TestUtils` is a test utility library that makes it easy to snapshot-test the public API surface of your assemblies. It uses reflection to extract all public types and their members, serialises the result to JSON, and pairs well with [Verify](https://github.com/VerifyTests/Verify) for approval-based testing.

## Overview

When combined with the [PublicApiAnalyzers](../public-api-analyzers/index.md) package, `TestUtils` provides a safety net that catches unintentional public API changes during code review. Any modification to your public surface will cause a snapshot diff, making breaking changes immediately visible in pull requests.

## Installation

```shell
dotnet add package Invex.RepoUtils.TestUtils
```

The package targets `netstandard2.0` and `net10.0`, so it works in test projects on `net10.0` and on
any other framework that supports `netstandard2.0`.

## Dependencies

- [JetBrains.Annotations](https://www.nuget.org/packages/JetBrains.Annotations) — for `[PublicAPI]` attribute
- [System.Text.Json](https://www.nuget.org/packages/System.Text.Json) — on `netstandard2.0` only, for serialization

The package does not depend on a snapshot library. Add one to your test project; the examples use
[Verify.NUnit](https://www.nuget.org/packages/Verify.NUnit).

## Usage

### Basic API Surface Snapshot

Call <xref:Invex.RepoUtils.TestUtils.PublicApiSurfaceTestUtil.GetPublicApiSurface(System.Reflection.Assembly)> with the assembly you want to inspect. The returned JSON string can be verified with your preferred snapshot testing library; with Verify, `VerifyJson` stores it as formatted JSON:

```csharp
using Invex.RepoUtils.TestUtils;

[TestFixture]
public class ApiSurfaceTests
{
    [Test]
    public Task VerifyPublicApiSurface() =>
        VerifyJson(PublicApiSurfaceTestUtil.GetPublicApiSurface(typeof(MyLibraryType).Assembly));
}
```

### What Gets Captured

The utility inspects the target assembly with reflection and records:

| Item | Captured information |
|------|---------------------|
| **Types** | Full name of every top-level public type (`Type.IsPublic`), sorted by name |
| **Members** | Name of every public instance and static field, property and method declared on the type, sorted by name |

Only names are recorded: member types, return types and parameters are not part of the snapshot, so a signature
change that keeps the member name does not change the output.

The following are **excluded**:

- Non-public types and members, and nested types
- Inherited members (only members declared on the type itself are listed)
- Special-name methods (property and event accessors, operators); the property or field itself is still listed

> [!NOTE]
> Public members the compiler generates are included. Records, for example, contribute `<Clone>$`, `Deconstruct`,
> `Equals`, `GetHashCode` and `ToString`, and enums contribute `value__`.

### Output Format

The output is a JSON array of types, each containing its public members:

```json
[
  {
    "Name": "MyNamespace.MyClass",
    "Members": [
      { "Name": "Execute" },
      { "Name": "Value" }
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
    <!-- netstandard2.0 or net10.0 (and anything that can consume them) is supported -->
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

## See also

- [Public API Analyzers](../public-api-analyzers/index.md)
- [Breaking Change Detection](../atom-module/breaking-changes.md)
- <xref:Invex.RepoUtils.TestUtils.PublicApiSurfaceTestUtil>
