using System.Diagnostics.CodeAnalysis;

namespace Invex.RepoUtils.PublicApiAnalyzers;

// ReSharper disable once InconsistentNaming
/// <summary>
///     Analyzer that reports public members that are not annotated with [PublicAPI] (and are not inside a type annotated
///     with [PublicAPI]).
/// </summary>
#pragma warning disable RS1038
[DiagnosticAnalyzer(LanguageNames.CSharp)]
[SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1036:Specify analyzer banned API enforcement setting")]
#pragma warning restore RS1038
public class IPAA0001_PublicMemberShouldBeAnnotatedWithPublicAPIAnalyzer : DiagnosticAnalyzer
{
    private const string DiagnosticId = "IPAA0001";

    // The category of the diagnostic (Design, Naming etc.).
    private const string Category = "Design";

    // Feel free to use raw strings if you don't need localization.
    private const string Title = "Public member should be annotated with [PublicAPI] or other valid attribute";

    // The message that will be displayed to the user.
    private const string MessageFormat = "Public member '{0}' is missing [PublicAPI] or other valid attribute";

    private const string Description =
        "Public members should be annotated with [PublicAPI] to indicate they are part of the public API surface, or another valid attribute.";

    private static readonly DiagnosticDescriptor Rule = new(DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        true,
        Description);

    // Keep in mind: you have to list your rules here.
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var configCache = new ConcurrentDictionary<string, ImmutableHashSet<string>>(StringComparer.Ordinal);

            compilationContext.RegisterSymbolAction(symbolContext => AnalyzeSymbol(symbolContext, configCache),
                SymbolKind.NamedType,
                SymbolKind.Method,
                SymbolKind.Property,
                SymbolKind.Field,
                SymbolKind.Event);
        });
    }

    private static void AnalyzeSymbol(
        SymbolAnalysisContext context,
        ConcurrentDictionary<string, ImmutableHashSet<string>> configCache)
    {
        var symbol = context.Symbol;

        if (symbol.IsImplicitlyDeclared)
            return;

        if (symbol is IMethodSymbol methodSymbol)
        {
            if (methodSymbol.MethodKind is MethodKind.Constructor
                or MethodKind.StaticConstructor
                or MethodKind.PropertyGet
                or MethodKind.PropertySet
                or MethodKind.EventAdd
                or MethodKind.EventRemove
                or MethodKind.EventRaise)
                return;

            if (methodSymbol.IsOverride)
                return;
        }

        if (!IsEffectivelyPublic(symbol))
            return;

        var validAttributes = GetValidAttributes(context, configCache);

        if (HasValidAttributeRecursive(symbol, validAttributes))
            return;

        if (symbol.Name == "" || validAttributes.Contains(symbol.Name))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, symbol.Locations[0], symbol.Name));
    }

    private static bool IsEffectivelyPublic(ISymbol symbol)
    {
        var current = symbol;

        while (current != null)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
                return false;

            current = current.ContainingType;
        }

        return true;
    }

    private static ImmutableHashSet<string> GetValidAttributes(
        SymbolAnalysisContext context,
        ConcurrentDictionary<string, ImmutableHashSet<string>> cache)
    {
        var optionsProvider = context.Options.AnalyzerConfigOptionsProvider;

        var tree = context.Symbol.Locations.Length > 0
            ? context.Symbol.Locations[0].SourceTree
            : null;

        if (tree != null &&
            optionsProvider
                .GetOptions(tree)
                .TryGetValue("dotnet_code_quality.DecSm_Analyzers_ValidPublicApiAttributes", out var treeValues))
            return ParseAndCache(treeValues, cache);

        optionsProvider.GlobalOptions.TryGetValue("dotnet_code_quality.DecSm_Analyzers_ValidPublicApiAttributes",
            out var globalValues);

        return ParseAndCache(globalValues ?? string.Empty, cache);
    }

    private static ImmutableHashSet<string> ParseAndCache(
        string values,
        ConcurrentDictionary<string, ImmutableHashSet<string>> cache) =>
        cache.GetOrAdd(values,
            v =>
            {
                var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
                builder.Add("PublicAPI");
                builder.Add("PublicAPIAttribute");

                if (!string.IsNullOrWhiteSpace(v))
                {
                    var parts = v.Split([','], StringSplitOptions.RemoveEmptyEntries);

                    foreach (var part in parts)
                    {
                        var trimmed = part.Trim();

                        if (string.IsNullOrWhiteSpace(trimmed))
                            continue;

                        builder.Add(trimmed);

                        if (!trimmed.EndsWith("Attribute", StringComparison.Ordinal))
                            builder.Add(trimmed + "Attribute");
                    }
                }

                return builder.ToImmutable();
            });

    private static bool HasValidAttributeRecursive(ISymbol symbol, ImmutableHashSet<string> validAttributes)
    {
        var current = symbol;

        while (current != null)
        {
            foreach (var attribute in current.GetAttributes())
                if (attribute.AttributeClass != null && validAttributes.Contains(attribute.AttributeClass.Name))
                    return true;

            current = current.ContainingType;
        }

        return false;
    }
}
