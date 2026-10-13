# Getting Started

This guide takes a library from no API checks to a full chain: the analyzer marks intentional API, a test
snapshots it, and an Atom target fails pull requests whose version bump does not match the change. Each step
is useful on its own, so stop wherever you like.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- For the analyzer: any C# project (the analyzer targets `netstandard2.0`)
- For the test utilities: a test project targeting `netstandard2.0`, `net8.0`, `net9.0` or `net10.0`
- For the Atom module: an [Atom](https://github.com/Invex-Games/atom) build project targeting `net8.0`, `net9.0` or `net10.0`

## 1. Declare your public API

Install the analyzer in each library project, together with a `[PublicAPI]` attribute:

```shell
dotnet add package Invex.RepoUtils.PublicApiAnalyzers
dotnet add package JetBrains.Annotations
```

The analyzer now runs during every build and reports `IPAA0001` for each public member that is not covered
by a valid attribute:

```csharp
using JetBrains.Annotations;

// Annotated: this type and its public members are intentionally part of the public API.
[PublicAPI]
public class MyService
{
    public string Name { get; set; } = "";

    public void Execute() { }
}

// IPAA0001: Public member 'InternalHelper' is missing [PublicAPI] or other valid attribute
public class InternalHelper { }
```

Either annotate `InternalHelper` or make it `internal`.

## 2. Snapshot the public surface

Add the test utilities and a snapshot library to your test project:

```shell
dotnet add package Invex.RepoUtils.TestUtils
dotnet add package Verify.NUnit
```

Then write one test per library assembly:

```csharp
using Invex.RepoUtils.TestUtils;

[TestFixture]
public class PublicApiTests
{
    [Test]
    public Task VerifyPublicApiSurface() =>
        VerifyJson(PublicApiSurfaceTestUtil.GetPublicApiSurface(typeof(MyService).Assembly));
}
```

Run `dotnet test`, accept the new snapshot, and commit `PublicApiTests.VerifyPublicApiSurface.verified.txt`.

## 3. Check pull requests in CI

Add the Atom module to your build project:

```shell
dotnet add _atom/_atom.csproj package Invex.RepoUtils.Atom.Module
```

Mix `ICheckPrForBreakingChanges` into your build definition and point it at the snapshot files:

```csharp
using Invex.RepoUtils.Atom.Module.Targets;

[BuildDefinition]
[GenerateEntryPoint]
internal interface IBuild : IWorkflowBuildDefinition, ICheckPrForBreakingChanges
{
    IEnumerable<RootedPath> ICheckPrForBreakingChanges.BreakingChangeFilesToCheck =>
        RootedFileSystem
            .Directory
            .GetFiles(RootedFileSystem.AtomRootDirectory / "tests", "*.verified.txt", SearchOption.AllDirectories)
            .Select(RootedFileSystem.CreateRootedPath);
}
```

Add the `CheckPrForBreakingChanges` target to your pull-request workflow (see the workflow example in
[Targets](atom-module/targets.md#icheckprforbreakingchanges)), then regenerate the workflow YAML with `atom gen`.
The target compares the snapshots against your latest `v{semver}` release tag and posts a GitHub check run.

## Next steps

- Learn about the [analyzer rules](public-api-analyzers/rules.md) and [configuration options](public-api-analyzers/configuration.md)
- See what the [snapshot captures](test-utils/index.md)
- Understand [breaking change detection](atom-module/breaking-changes.md)
- Add [Dependabot auto-merge](atom-module/dependabot.md) and the other [targets](atom-module/targets.md)
