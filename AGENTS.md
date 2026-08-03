# Agent Instructions

Guidance for AI agents working in **Invex.RepoUtils** — a collection of .NET utilities for building
and maintaining .NET repositories. Keep changes focused; the README documents consumer-facing usage.

## Repository map

| Project | Role |
|---------|------|
| `Invex.RepoUtils.PublicApiAnalyzers` | Roslyn analyzer enforcing `[PublicAPI]` annotation (`netstandard2.0`). |
| `Invex.RepoUtils.TestUtils` | Public API snapshot-testing utilities (`netstandard2.0`, `net8.0`, `net9.0`, `net10.0`). |
| `Invex.RepoUtils.Atom.Module` | [Atom](https://github.com/Invex-Games/atom) CI/CD module (`net8.0`, `net9.0`, `net10.0`). |

Sources are under `src/`, tests under `tests/`, the Atom build definition under `_atom/`, and the
DocFX site under `docs/`. The solution is `Invex.RepoUtils.slnx`.

## Build, test & cleanup

The .NET 10 SDK is required by `global.json`; test projects target `net8.0`, `net9.0`, and
`net10.0`. C# `LangVersion 14`, `ImplicitUsings`, and `Nullable` are enabled, and
`TreatWarningsAsErrors` is on.

```shell
dotnet build Invex.RepoUtils.slnx
dotnet test Invex.RepoUtils.slnx
```

After making C# changes, run ReSharper cleanup over the solution:

```powershell
$sdk = dotnet --version
jb cleanupcode Invex.RepoUtils.slnx --include="**.cs" --toolset-path="C:\Program Files\dotnet\sdk\$sdk\MSBuild.dll"
```

- If `jb` is not installed, acquire it with `dotnet tool install -g JetBrains.ReSharper.GlobalTools`.
- `--toolset-path` is required. It points cleanup at the SDK selected by `global.json` instead of
  Visual Studio BuildTools MSBuild, avoiding `MSB4236` workload-resolution errors.
- Cleanup honors `.editorconfig` and shared `*.DotSettings`; no additional flags are needed.
- Global usings live in each project's `_usings.cs`; add shared usings there, not per-file.
- `GenerateDocumentationFile` is enabled. Add accurate XML documentation to every public type and
  member even though `CS1591` is suppressed.

## Atom workflows and generated files

The YAML under `.github/workflows/` (`Validate.yml`, `Build.yml`, `Cleanup Prereleases.yml`, and
`Dependabot Enable auto-merge.yml`) is generated from `_atom/IBuild.cs` and the module interfaces it
inherits.

When changing targets, workflow definitions, triggers, options, parameters/secrets, or inherited
interfaces, regenerate the YAML:

```shell
atom gen
# equivalently
dotnet run --project _atom/_atom.csproj -- gen
```

Commit generated workflow changes with the source change; never hand-edit generated YAML. Drift
between `_atom/` and `.github/workflows/` means `atom gen` is missing.

## Code and documentation conventions

- Annotate every new public member with `[PublicAPI]` where the in-repo analyzer applies.
- Match the existing `<summary>`/`<param>`/`<remarks>` style, especially in
  `src/Invex.RepoUtils.Atom.Module/Helpers`.
- Keep public documentation exact about nullability, false-return cases, retries, timeouts, and
  caching behavior.
- Keep internal cross-member helpers `internal`; expose only the intended consumer surface.
- Use [Conventional Commits](https://www.conventionalcommits.org/). `GitVersion.yml` maps
  `breaking:`/`major:` to major, `feat:`/`feature:`/`minor:` to minor,
  `fix:`/`patch:` to patch, and `semver-none`/`semver-skip` to no version bump.
- When adding user-facing features, update the relevant `docs/` page and `README.md`.

## Testing and Verify snapshots

- Tests use **NUnit** with Verify (`Verify.NUnit`) for snapshot/approval testing.
- A snapshot mismatch produces a `*.received.txt` next to the committed `*.verified.txt`.
- If the mismatch is unintended, fix the code. If it is intentional, replace the matching
  `*.verified.txt` with `*.received.txt`, delete the received file, and rerun the tests.
- Preserve LF (`\n`) line endings in verified files.
- The public API snapshot (for example,
  `tests/Invex.RepoUtils.Atom.Module.Tests/PublicApiTests.VerifyPublicApiSurface.verified.txt`)
  changes whenever the public API changes; review such diffs deliberately.

## Change checklist

1. Follow existing patterns and make a focused change.
2. Add XML docs and `[PublicAPI]` annotations where applicable.
3. Build and test the solution.
4. Update intentional Verify snapshots.
5. Run `jb cleanupcode` with the SDK `MSBuild.dll`.
6. Update consumer documentation for user-facing changes.
7. Run `atom gen` when generated workflows are affected.

## Defer to the docs

For details beyond these instructions, use:

- [README.md](README.md) — package overview and quick start.
- [docs/getting-started.md](docs/getting-started.md) — package installation.
- [docs/contributing.md](docs/contributing.md) — setup and pull-request guidance.
- [docs/atom-module/](docs/atom-module) — Atom targets, helpers, and breaking-change detection.
- [docs/public-api-analyzers/](docs/public-api-analyzers) — analyzer rules and configuration.
- [docs/test-utils/](docs/test-utils) — API surface snapshots.
- [docs/versioning.md](docs/versioning.md) — Conventional Commit and branch behavior.
