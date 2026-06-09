# Analyzer Rules

## IPAA0001: Public member should be annotated with [PublicAPI]

| Property    | Value |
|-------------|-------|
| **Rule ID** | IPAA0001 |
| **Category** | Design |
| **Default Severity** | Warning |
| **Enabled by default** | Yes |

### Description

Public members should be annotated with `[PublicAPI]` to indicate they are part of the public API surface, or another valid attribute. This ensures that every public API exposure is a deliberate, reviewable decision.

### When is a diagnostic reported?

A diagnostic is reported when **all** of the following are true:

1. The symbol is **effectively public** — it has `public` accessibility all the way up its containing type chain.
2. The symbol is **not** annotated with a valid attribute (e.g., `[PublicAPI]`), and none of its containing types are annotated.
3. The symbol is **not** one of the following excluded kinds:
   - Implicitly declared members (compiler-generated)
   - Property getters/setters
   - Event accessors (add/remove/raise)
   - Constructors (instance or static)
   - `override` methods

### Examples

#### Violation

```csharp
// ⚠️ IPAA0001: Public member 'MyService' is missing [PublicAPI] or other valid attribute
public class MyService
{
    // ⚠️ IPAA0001: Public member 'Execute' is missing [PublicAPI] or other valid attribute
    public void Execute() { }
}
```

#### Fix — annotate the type

```csharp
using JetBrains.Annotations;

// ✅ No diagnostic — type is annotated, so all its public members are covered
[PublicAPI]
public class MyService
{
    public void Execute() { }
}
```

#### Fix — annotate individual members

```csharp
using JetBrains.Annotations;

[PublicAPI]
public class MyService
{
    // Individual member annotation is also fine when the type is already annotated
    [PublicAPI]
    public void Execute() { }
}
```

### Attribute inheritance

The analyzer walks up the type hierarchy. If a containing type has a valid attribute, all its members are considered annotated:

```csharp
[PublicAPI]
public class Outer
{
    // ✅ No diagnostic — Outer is annotated
    public class Inner
    {
        // ✅ No diagnostic — Outer is annotated
        public void DoWork() { }
    }
}
```

### What counts as a "valid attribute"?

By default, the following attribute names are accepted:

- `PublicAPI`
- `PublicAPIAttribute`

You can extend this list via [configuration](configuration.md).

### How to suppress

```csharp
// Option 1: Suppress with a pragma
#pragma warning disable IPAA0001
public class SuppressedClass { }
#pragma warning restore IPAA0001

// Option 2: Suppress with an attribute
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "IPAA0001")]
public class SuppressedClass { }
```

Or change the severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.IPAA0001.severity = none
```

