# Breaking Change Detection

The Atom module includes a comprehensive breaking change detection system that compares your public API surface across versions and enforces appropriate version bumps.

## How It Works

```
┌─────────────────────────────────────────────────────────────┐
│                    PR is opened / updated                     │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  1. Find latest release tag (v{semver}) as baseline          │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  2. Diff API definition files between baseline and HEAD      │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  3. Classify changes as major (removals) or minor (additions)│
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  4. Verify version has been bumped appropriately             │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  5. Post GitHub check run (pass/fail) with detailed summary  │
└─────────────────────────────────────────────────────────────┘
```

## Change Classification

| Change Type | Trigger | Required Version Bump |
|-------------|---------|----------------------|
| **Major** | Lines removed from API definition files | Major version increment |
| **Minor** | Lines added to API definition files | Minor version increment |
| **None** | No changes to API files | No bump required |

### Exceptions

- Lines that only differ by a trailing/leading comma (list reformatting) are **not** classified as major changes.
- When no previous release tag exists, the check is skipped entirely.

## Version Validation Rules

| Scenario | Outcome |
|----------|---------|
| Major changes detected + major version bumped | ✅ Check passes |
| Major changes detected + major version NOT bumped | ❌ Check fails |
| Minor changes detected + minor (or major) version bumped | ✅ Check passes |
| Minor changes detected + neither minor nor major bumped | ❌ Check fails |
| No breaking changes detected | ✅ Check passes |

## API Definition Files

The system works with text-based API definition files — typically the `.verified.txt` files produced by [snapshot tests](../test-utils/index.md). Configure which files to track by implementing `BreakingChangeFilesToCheck`:

```csharp
IEnumerable<RootedPath> ICheckPrForBreakingChanges.BreakingChangeFilesToCheck =>
    RootedFileSystem
        .Directory
        .GetFiles(RootedFileSystem.AtomRootDirectory / "tests", "*.verified.txt", SearchOption.AllDirectories)
        .Select(RootedFileSystem.CreateRootedPath);
```

## GitHub Check Run Output

The check run posts a detailed markdown summary to the pull request:

### ✅ No Breaking Changes

> ✅ **No Breaking Changes Detected**
>
> This pull request does not contain any breaking changes to the public API surface.
> Safe to merge without version bump considerations.

### ✅ Major Changes (version bumped)

> ℹ️ **Major Breaking Changes Detected**
>
> This pull request contains major breaking changes to the public API surface.
>
> **Version Bump Status:** ✅ Major version has been bumped from `1` to `2`

### ❌ Major Changes (version NOT bumped)

> ⚠️ **Major Breaking Changes Detected - Action Required**
>
> This pull request contains major breaking changes to the public API surface, but the major version has not been bumped.
>
> **Required Action:** Please increment the major version number before merging this pull request.

## Integration with Versioning

This system works hand-in-hand with [GitVersion](../versioning.md). Use conventional commit prefixes to trigger the appropriate version bump:

```
# For major breaking changes
breaking: remove deprecated MyService.OldMethod API

# For minor additions
feat: add new MyService.NewMethod to public API
```

