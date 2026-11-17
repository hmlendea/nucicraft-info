using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace NuciCraftInfo.ArchitectureTests
{
    [TestFixture]
    public sealed class ArchitectureTestSourceConventionTests
    {
        [Test]
        public void GivenArchitectureTestSources_WhenChecked_ThenEveryFileDeclaresOneTypeWithAMatchingName()
        {
            foreach (string source in SourceSyntax.GetSources(ArchitectureResources.ArchitectureTestSourcePrefix))
            {
                SyntaxTree tree = SourceSyntax.ParseSource(ArchitectureResources.ArchitectureTestSourcePrefix, source);

                BaseTypeDeclarationSyntax[] declarations = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BaseTypeDeclarationSyntax>()
                    .Where(declaration => declaration.Parent is CompilationUnitSyntax ||
                        declaration.Parent is BaseNamespaceDeclarationSyntax)
                    .ToArray();

                Assert.That(declarations, Has.Length.EqualTo(1), $"{source}: architecture test files must declare exactly one top-level type.");
                Assert.That(
                    declarations[0].Identifier.ValueText,
                    Is.EqualTo(GetFileStem(source)),
                    $"{source}: the declared type must match the file name.");
            }
        }

        [Test]
        public void GivenArchitectureTestSources_WhenChecked_ThenTheyUseBlockNamespaces()
        {
            foreach (string source in SourceSyntax.GetSources(ArchitectureResources.ArchitectureTestSourcePrefix))
            {
                SyntaxTree tree = SourceSyntax.ParseSource(ArchitectureResources.ArchitectureTestSourcePrefix, source);

                Assert.That(
                    tree.GetRoot().DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>(),
                    Is.Empty,
                    $"{source}: architecture test files must use block namespaces.");
            }
        }

        [Test]
        public void GivenArchitectureTestSources_WhenChecked_ThenTheNamespaceMatchesTheFolderPath()
        {
            foreach (string source in SourceSyntax.GetSources(ArchitectureResources.ArchitectureTestSourcePrefix))
            {
                SyntaxTree tree = SourceSyntax.ParseSource(ArchitectureResources.ArchitectureTestSourcePrefix, source);

                NamespaceDeclarationSyntax[] namespaces = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<NamespaceDeclarationSyntax>()
                    .ToArray();

                Assert.That(namespaces, Has.Length.EqualTo(1), $"{source}: the file must declare exactly one block namespace.");
                Assert.That(
                    namespaces[0].Name.ToString(),
                    Is.EqualTo("NuciCraftInfo.ArchitectureTests"),
                    $"{source}: the architecture test namespace must be NuciCraftInfo.ArchitectureTests.");
            }
        }

        [Test]
        public void GivenArchitectureTestSources_WhenChecked_ThenEveryTestDeclaresAnAssertion()
        {
            foreach (string source in SourceSyntax.GetSources(ArchitectureResources.ArchitectureTestSourcePrefix))
            {
                SyntaxTree tree = SourceSyntax.ParseSource(ArchitectureResources.ArchitectureTestSourcePrefix, source);

                List<string> assertionFreeTests = [];

                foreach (MethodDeclarationSyntax method in tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => HasAttribute(method.AttributeLists, "Test") || HasAttribute(method.AttributeLists, "TestCase")))
                {
                    bool asserts = method.DescendantNodes()
                        .OfType<InvocationExpressionSyntax>()
                        .Any(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                            (memberAccess.Expression.ToString().StartsWith("Assert", StringComparison.Ordinal) ||
                                memberAccess.Name.Identifier.ValueText == "Check" ||
                                memberAccess.Name.Identifier.ValueText == "HasNoViolations"));

                    if (!asserts)
                    {
                        assertionFreeTests.Add(method.Identifier.ValueText);
                    }
                }

                Assert.That(
                    assertionFreeTests,
                    Is.Empty,
                    $"{source}: every architecture test must assert -> {string.Join(", ", assertionFreeTests)}.");
            }
        }

        private static bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
            => attributeLists
                .SelectMany(attributeList => attributeList.Attributes)
                .Any(attribute => string.Equals(attribute.Name.ToString(), attributeName, StringComparison.Ordinal));

        private static string GetFileStem(string source)
        {
            string fileName = source.Substring(source.LastIndexOf('/') + 1);

            return fileName.Substring(0, fileName.LastIndexOf('.'));
        }
    }
}