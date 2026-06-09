using Verifier = Invex.RepoUtils.PublicApiAnalyzers.Tests.ExtendedAnalyzerVerifier<Invex.RepoUtils.PublicApiAnalyzers.IPAA0001_PublicMemberShouldBeAnnotatedWithPublicAPIAnalyzer>;

namespace Invex.RepoUtils.PublicApiAnalyzers.Tests;

// ReSharper disable once InconsistentNaming
public class IPAA0001_PublicMemberShouldBeAnnotatedWithPublicAPIAnalyzerTests
{
    private void Configure(
        CSharpAnalyzerTest<IPAA0001_PublicMemberShouldBeAnnotatedWithPublicAPIAnalyzer, DefaultVerifier> configuration)
    {
        configuration.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId);

            if (project == null)
                return solution; // Should not happen in normal test execution

            // Get the existing parse options and update the language version
            var parseOptions = (CSharpParseOptions)project.ParseOptions!;

            var updatedParseOptions = parseOptions.WithLanguageVersion(LanguageVersion.CSharp14);

            // Return the solution with the updated parse options for the project
            return solution.WithProjectParseOptions(projectId, updatedParseOptions);
        });

        configuration.ReferenceAssemblies = ReferenceAssemblies.Net.Net100;

        // We need to define PublicAPIAttribute for the test, as it's not in the standard library
        configuration.TestState.Sources.Add(("PublicAPIAttribute.cs", """
                                                                      using System;

                                                                      [AttributeUsage(AttributeTargets.All, Inherited = false)]
                                                                      public sealed class PublicAPIAttribute : Attribute
                                                                      {
                                                                          public PublicAPIAttribute() { }
                                                                          public PublicAPIAttribute(string comment) { }
                                                                      }
                                                                      """));
    }

    [Fact]
    public async Task PublicClassWithoutAttribute_AlertDiagnostic()
    {
        const string text = """
                            public class MyClass
                            {
                            }
                            """;

        var expected = Verifier
            .Diagnostic()
            .WithSpan(1, 14, 1, 21)
            .WithArguments("MyClass");

        await Verifier.VerifyAnalyzerAsync(text, Configure, expected);
    }

    [Fact]
    public async Task PublicClassWithAttribute_NoDiagnostic()
    {
        const string text = """
                            [PublicAPI]
                            public class MyClass
                            {
                            }
                            """;

        await Verifier.VerifyAnalyzerAsync(text, Configure);
    }

    [Fact]
    public async Task PublicMemberInPublicClassWithoutAttribute_AlertDiagnostic()
    {
        const string text = """
                            public class MyClass
                            {
                                public void MyMethod() { }
                            }
                            """;

        DiagnosticResult[] expected =
        [
            Verifier
                .Diagnostic()
                .WithSpan(1, 14, 1, 21)
                .WithArguments("MyClass"),
            Verifier
                .Diagnostic()
                .WithSpan(3, 17, 3, 25)
                .WithArguments("MyMethod"),
        ];

        await Verifier.VerifyAnalyzerAsync(text, Configure, expected);
    }

    [Fact]
    public async Task PublicMemberInPublicClassWithAttribute_NoDiagnostic()
    {
        const string text = """
                            [PublicAPI]
                            public class MyClass
                            {
                                public void MyMethod() { }
                            }
                            """;

        await Verifier.VerifyAnalyzerAsync(text, Configure);
    }

    [Fact]
    public async Task PublicMemberWithAttributeInPublicClassWithoutAttribute_AlertDiagnosticForClassOnly()
    {
        const string text = """
                            public class MyClass
                            {
                                [PublicAPI]
                                public void MyMethod() { }
                            }
                            """;

        var expected = Verifier
            .Diagnostic()
            .WithSpan(1, 14, 1, 21)
            .WithArguments("MyClass");

        await Verifier.VerifyAnalyzerAsync(text, Configure, expected);
    }

    [Fact]
    public async Task InternalClass_NoDiagnostic()
    {
        const string text = """
                            internal class MyClass
                            {
                                public void MyMethod() { }
                            }
                            """;

        await Verifier.VerifyAnalyzerAsync(text, Configure);
    }

    [Fact]
    public async Task PrivateMember_NoDiagnostic()
    {
        const string text = """
                            public class MyClass
                            {
                                private void MyMethod() { }
                            }
                            """;

        var expected = Verifier
            .Diagnostic()
            .WithSpan(1, 14, 1, 21)
            .WithArguments("MyClass");

        await Verifier.VerifyAnalyzerAsync(text, Configure, expected);
    }

    [Fact]
    public async Task NestedPublicClassInPublicAPIClass_NoDiagnostic()
    {
        const string text = """
                            [PublicAPI]
                            public class MyClass
                            {
                                public class NestedClass
                                {
                                    public void MyMethod() { }
                                }
                            }
                            """;

        await Verifier.VerifyAnalyzerAsync(text, Configure);
    }

    [Fact]
    public async Task NestedPublicClassWithoutAttributeInClassWithoutAttribute_AlertDiagnostic()
    {
        const string text = """
                            public class MyClass
                            {
                                public class NestedClass
                                {
                                    public void MyMethod() { }
                                }
                            }
                            """;

        DiagnosticResult[] expected =
        [
            Verifier
                .Diagnostic()
                .WithSpan(1, 14, 1, 21)
                .WithArguments("MyClass"),
            Verifier
                .Diagnostic()
                .WithSpan(3, 18, 3, 29)
                .WithArguments("NestedClass"),
            Verifier
                .Diagnostic()
                .WithSpan(5, 21, 5, 29)
                .WithArguments("MyMethod"),
        ];

        await Verifier.VerifyAnalyzerAsync(text, Configure, expected);
    }

    [Fact]
    public async Task ExtensionBlock_NoDiagnostic()
    {
        const string text = """
                            public static class MyClass
                            {
                                extension(MyClass)
                                {
                                    // ...
                                }
                            }
                            """;

        DiagnosticResult[] expected =
        [
            Verifier
                .Diagnostic()
                .WithSpan(1, 21, 1, 28)
                .WithArguments("MyClass"),
        ];

        await Verifier.VerifyAnalyzerAsync(text, Configure, expected);
    }

    [Fact]
    public async Task PublicMemberInNestedInternalClass_NoDiagnostic()
    {
        const string text = """
                            public class Outer
                            {
                                internal class Inner
                                {
                                    public void MyMethod() { }
                                }
                            }
                            """;

        var expected = Verifier
            .Diagnostic()
            .WithSpan(1, 14, 1, 19)
            .WithArguments("Outer");

        await Verifier.VerifyAnalyzerAsync(text, Configure, expected);
    }

    [Fact]
    public async Task PublicMemberInPublicClassInInternalClass_NoDiagnostic()
    {
        const string text = """
                            internal class Outer
                            {
                                public class Inner
                                {
                                    public void MyMethod() { }
                                }
                            }
                            """;

        await Verifier.VerifyAnalyzerAsync(text, Configure);
    }

    [Fact]
    public async Task CustomAttribute_WhenConfigured_NoDiagnostic()
    {
        const string text = """
                            using System;

                            [AttributeUsage(AttributeTargets.All, Inherited = false)]
                            public sealed class PublicAPIAttribute : Attribute { }

                            [AttributeUsage(AttributeTargets.All, Inherited = false)]
                            [PublicAPI]
                            public sealed class MyCustomAttribute : Attribute { }

                            [MyCustom]
                            public class MyClass
                            {
                            }
                            """;

        var test = new CSharpAnalyzerTest<IPAA0001_PublicMemberShouldBeAnnotatedWithPublicAPIAnalyzer, DefaultVerifier>
        {
            TestCode = text,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
        };

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var documentId = DocumentId.CreateNewId(projectId);

            return solution.AddAnalyzerConfigDocument(documentId,
                ".editorconfig",
                SourceText.From(
                    "is_global = true\r\ndotnet_code_quality.DecSm_Analyzers_ValidPublicApiAttributes = MyCustom"),
                filePath: "/.editorconfig");
        });

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CustomAttribute_WhenNotConfigured_Diagnostic()
    {
        const string text = """
                            using System;

                            [AttributeUsage(AttributeTargets.All, Inherited = false)]
                            public sealed class PublicAPIAttribute : Attribute { }

                            [AttributeUsage(AttributeTargets.All, Inherited = false)]
                            [PublicAPI]
                            public sealed class MyCustomAttribute : Attribute { }

                            [MyCustom]
                            public class MyClass
                            {
                            }
                            """;

        var test = new CSharpAnalyzerTest<IPAA0001_PublicMemberShouldBeAnnotatedWithPublicAPIAnalyzer, DefaultVerifier>
        {
            TestCode = text,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
        };

        test.ExpectedDiagnostics.Add(DiagnosticResult
            .CompilerWarning("IPAA0001")
            .WithSpan(11, 14, 11, 21)
            .WithArguments("MyClass"));

        await test.RunAsync(CancellationToken.None);
    }
}
