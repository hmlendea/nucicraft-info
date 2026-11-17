using System;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace NuciCraftInfo.ArchitectureTests
{
    [TestFixture]
    public sealed class NamespaceAndUsingConventionTests
    {
        [Test]
        public void GivenProductionSources_WhenChecked_ThenUsingsAppearAboveAndOutsideTheNamespace()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);
                CompilationUnitSyntax root = (CompilationUnitSyntax)tree.GetRoot();

                Assert.That(
                    root.Usings.Count,
                    Is.GreaterThan(0),
                    $"{source}: the file must declare its using directives outside the namespace.");

                foreach (NamespaceDeclarationSyntax namespaceDeclaration in root.DescendantNodes().OfType<NamespaceDeclarationSyntax>())
                {
                    Assert.That(
                        namespaceDeclaration.Usings,
                        Is.Empty,
                        $"{source}: using directives must be placed above and outside the namespace.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenGroupedUsingsAreOrderedByTheDocumentedGroups()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);
                CompilationUnitSyntax root = (CompilationUnitSyntax)tree.GetRoot();

                int[] groupOrders = root.Usings
                    .Select(usingDirective => GetUsingGroupOrder(usingDirective.Name?.ToString() ?? string.Empty))
                    .ToArray();

                Assert.That(groupOrders, Is.Ordered, $"{source}: using groups must be ordered as System.*, Microsoft.*, third-party, then NuciCraftInfo.*.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenEveryTypeResidesInTheDocumentedRootNamespace()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                foreach (NamespaceDeclarationSyntax namespaceDeclaration in tree.GetRoot().DescendantNodes().OfType<NamespaceDeclarationSyntax>())
                {
                    string nameSpace = namespaceDeclaration.Name.ToString();

                    Assert.That(
                        nameSpace == RootNamespace || nameSpace.StartsWith(RootNamespace + ".", StringComparison.Ordinal),
                        Is.True,
                        $"{source}: every type must reside in the root namespace or a child namespace.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoUtilityOrHelperNamespacesAreDeclared()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                string[] forbiddenNamespaces = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BaseNamespaceDeclarationSyntax>()
                    .Select(declaration => declaration.Name.ToString())
                    .Where(IsForbiddenNamespace)
                    .ToArray();

                Assert.That(forbiddenNamespaces, Is.Empty, $"{source}: catch-all Helpers, Utils, Common, Misc, or Shared namespaces are not allowed.");
            }
        }

        private static string RootNamespace => "NuciCraftInfo";

        private static bool IsForbiddenNamespace(string nameSpace)
        {
            string[] forbiddenSegments = ["Helpers", "Utils", "Common", "Misc", "Shared"];

            return nameSpace
                .Split('.')
                .Any(segment => forbiddenSegments.Contains(segment, StringComparer.Ordinal));
        }

        private static int GetUsingGroupOrder(string nameSpace)
        {
            if (nameSpace.StartsWith("System", StringComparison.Ordinal))
            {
                return 0;
            }

            if (nameSpace.StartsWith("Microsoft", StringComparison.Ordinal))
            {
                return 1;
            }

            if (nameSpace.StartsWith("Nuci", StringComparison.Ordinal))
            {
                return 3;
            }

            return 2;
        }
    }
}