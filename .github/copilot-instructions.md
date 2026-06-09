# Copilot Instructions

Guidance for AI agents working in **Invex.RepoUtils** — a collection of .NET utilities for building
and maintaining .NET repositories. Keep changes focused and defer to the linked docs for detail.

## What's in the repo

| Project | Role |
|---------|------|
| `Invex.RepoUtils.PublicApiAnalyzers` | Roslyn analyzer enforcing `[PublicAPI]` annotation of the public API surface (`netstandard2.0`). |
| `Invex.RepoUtils.TestUtils` | Test utilities for snapshot-testing an assembly's public API surface (`net10.0`). |
| `Invex.RepoUtils.Atom.Module` | [Atom](https://github.com/Invex-Games/atom) build module with reusable CI/CD targets/helpers (`net10.0`). |

Sources live under `src/`, tests under `tests/`, the Atom build definition under `_atom/`, and the
DocFX documentation site under `docs/`.

## Build & language specifics

- **.NET 10 SDK** is required. Test projects multi-target `net8.0;net9.0;net10.0`.
- C# `LangVersion 14`, `ImplicitUsings` and `Nullable` enabled, `TreatWarningsAsErrors` on.
- Global usings live in each project's `_usings.cs` — add shared usings there, not per-file.
- `GenerateDocumentationFile` is on, so **all public members need XML doc comments**.
- Build and test the whole solution:

  ```shell
  dotnet build Invex.RepoUtils.slnx
  dotnet test
  ```

## Atom workflows

- The GitHub Actions workflow YAML under `.github/workflows/` (`Validate.yml`, `Build.yml`,
  `Dependabot Enable auto-merge.yml`) is **generated** from the Atom build definition in `_atom/`
  (`IBuild.cs` and the module interfaces it inherits, e.g. those in
  `src/Invex.RepoUtils.Atom.Module`).
- **Whenever you change anything that affects the workflows** — targets, workflow definitions,
  triggers, options, params/secrets, or the interfaces `IBuild` inherits — regenerate the YAML:

  ```shell
  atom gen
  ```

  (equivalently `dotnet run --project _atom -- gen`). Commit the regenerated `.github/workflows/`
  files alongside your `_atom/` changes; **never hand-edit the generated YAML**.
- A drift between `_atom/` and the committed YAML should be treated as a missing `atom gen` run.

## Conventions

- Annotate every new public member with `[PublicAPI]` — the in-repo analyzer flags anything missing,
  and warnings are errors.
- Add XML doc comments to all public types and members (match the existing `<summary>`/`<param>`/
  `<remarks>` style in `src/Invex.RepoUtils.Atom.Module/Helpers`).
- Use [Conventional Commits](https://www.conventionalcommits.org/) — the prefix drives versioning
  (`feat:`, `fix:`, `breaking:`, `docs:`, …).
- When adding user-facing features, update the relevant `docs/` page and the `README.md`.

## Testing & the Verify workflow

- Tests use **NUnit** with **Verify** (`Verify.NUnit`) for snapshot/approval testing.
- A snapshot test fails when its output differs from the committed `*.verified.txt`. On failure,
  Verify writes a `*.received.txt` next to it.
- **If the diff is unintended**, fix the code. **If the change is valid (expected new output)**,
  accept it and re-run:
  1. Overwrite the `*.verified.txt` with the contents of the matching `*.received.txt`.
  2. Delete the `*.received.txt`.
  3. Re-run `dotnet test` to confirm the suite is green.
- The public API surface snapshot (e.g. `tests/Invex.RepoUtils.Atom.Module.Tests/
  PublicApiTests.VerifyPublicApiSurface.verified.txt`) changes whenever the public API changes —
  treat an unexpected diff here as a signal to double-check the API change is intentional.

## Defer to the docs

For anything beyond the above, prefer these over duplicating detail:

- [README.md](../README.md) — package overview and quick start.
- [docs/contributing.md](../docs/contributing.md) — setup, guidelines, PR process, CI/CD.
- [docs/atom-module/](../docs/atom-module) — Atom targets, helpers, breaking-change detection.
- [docs/public-api-analyzers/](../docs/public-api-analyzers) — analyzer rules and configuration.
- [docs/versioning.md](../docs/versioning.md) — Conventional Commit → version bump mapping.

