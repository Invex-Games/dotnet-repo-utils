# Versioning

Versions in this repository are derived automatically by [GitVersion](https://gitversion.net/) using [Conventional Commits](https://www.conventionalcommits.org/).

## Commit Message Prefixes

The commit message prefix drives the version bump:

| Prefix | Bump | Example |
|--------|------|---------|
| `breaking:` / `major:` | Major | `breaking: remove deprecated API method` |
| `feat:` / `feature:` / `minor:` | Minor | `feat: add new analyzer rule` |
| `fix:` / `patch:` | Patch | `fix: correct false positive in analyzer` |
| `semver-none` / `semver-skip` | None | `semver-none: update documentation only` |

Scoped prefixes are also supported:

```
feat(analyzer): add support for record types
fix(atom-module): handle missing release tags gracefully
breaking(test-utils): change return type of GetPublicApiSurface
```

## Branch Strategy

The repository uses the following branch strategy with GitVersion:

| Branch Pattern | Label | Increment | Pre-release Weight |
|---------------|-------|-----------|-------------------|
| `main` | `rc` | Minor | 4000–4999 |
| `patch/**` | `rc` | Patch | 5000–5999 |
| `dev` / `develop` | `preview` | Minor | 3000–3999 |
| `feat/**` / `fix/**` / `chore/**` | `beta.{type}-{name}` | Minor | 2000–2999 |
| `pr/**` | `pr` | Inherit | 0–999 |
| Tagged releases | — | — | 6000 (highest) |

## Version Format

- **Assembly informational version:** `{Major}.{Minor}.{Patch}{PreReleaseTagWithDash}`
  - Example: `2.1.0-rc.1`
- **Assembly file version:** `{Major}.{Minor}.{Patch}.{WeightedPreReleaseNumber}`
  - Example: `2.1.0.4001`

## Release Process

1. Merge your PR into `main` with an appropriate conventional commit prefix.
2. GitVersion calculates the new version automatically.
3. The **Build** workflow creates a NuGet package with the computed version.
4. To publish a stable release, create a GitHub Release with a `v{semver}` tag (e.g., `v2.1.0`).
5. The release event triggers the Build workflow, which publishes to NuGet and attaches packages to the GitHub Release.

## Examples

```shell
# Patch release (1.2.3 → 1.2.4)
git commit -m "fix: resolve null reference in analyzer"

# Minor release (1.2.3 → 1.3.0)
git commit -m "feat: add configurable attribute list"

# Major release (1.2.3 → 2.0.0)
git commit -m "breaking: rename PublicAPI attribute to PublicApiAttribute"

# No version change
git commit -m "semver-none: update CI workflow"
```

