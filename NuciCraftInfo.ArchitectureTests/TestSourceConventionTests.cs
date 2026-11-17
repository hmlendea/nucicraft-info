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
    public sealed class TestSourceConventionTests
    {
        [Test]
        public void GivenTestSources_WhenChecked_ThenEveryFileDeclaresOneTypeMatchingItsName()
        {
            foreach (string source in GetTestSourcePaths())
            {
                SyntaxTree tree = ParseTree(source);
                BaseTypeDeclarationSyntax[] declarations = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BaseTypeDeclarationSyntax>()
                    .ToArray();

                Assert.That(declarations, Has.Length.EqualTo(1), $"{source}: the file must declare exactly one type.");
                Assert.That(
                    declarations[0].Identifier.ValueText,
                    Is.EqualTo(GetFileStem(source)),
                    $"{source}: the declared type must match the file name.");
            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenTheyUseBlockNamespacesMatchingTheFolderPath()
        {
            foreach (string source in GetTestSourcePaths())
            {
                SyntaxTree tree = ParseTree(source);

                Assert.That(
                    tree.GetRoot().DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>(),
                    Is.Empty,
                    $"{source}: test sources must use block namespaces.");

                NamespaceDeclarationSyntax[] namespaces = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<NamespaceDeclarationSyntax>()
                    .ToArray();

                Assert.That(namespaces, Has.Length.EqualTo(1), $"{source}: the file must declare exactly one block namespace.");
                Assert.That(
                    namespaces[0].Name.ToString(),
                    Is.EqualTo(GetExpectedNamespace(source)),
                    $"{source}: the test namespace must follow the folder path.");
            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenEveryFixtureIsNUnitAndSealed()
        {
            foreach (string source in GetTestSourcePaths())
            {
                SyntaxTree tree = ParseTree(source);

                foreach (ClassDeclarationSyntax fixture in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    if (!HasTests(fixture))
                    {
                        continue;
                    }

                    Assert.That(
                        HasAttribute(fixture.AttributeLists, "TestFixture"),
                        Is.True,
                        $"{source}: the fixture '{fixture.Identifier.ValueText}' must declare [TestFixture].");

                    Assert.That(
                        fixture.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.SealedKeyword)),
                        Is.True,
                        $"{source}: the fixture '{fixture.Identifier.ValueText}' must be sealed.");

                    Assert.That(
                        fixture.Identifier.ValueText.EndsWith("Tests", StringComparison.Ordinal),
                        Is.True,
                        $"{source}: the fixture '{fixture.Identifier.ValueText}' must end with 'Tests'.");
                }
            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenEveryTestMethodFollowsTheGivenWhenThenNaming()
        {
            foreach (string source in GetTestSourcePaths())
            {
                SyntaxTree tree = ParseTree(source);

                foreach (MethodDeclarationSyntax method in GetTestMethods(tree))
                {
                    Assert.That(
                        method.Identifier.ValueText,
                        Does.StartWith("Given").And.Contain("_When").And.Contain("_Then"),
                        $"{source}: the test '{method.Identifier.ValueText}' must use Given..._When..._Then... naming.");
                }
            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenParameterisedTestsDeclareAtLeastTwoCases()
        {
            foreach (string source in GetTestSourcePaths())
            {
                SyntaxTree tree = ParseTree(source);

                foreach (MethodDeclarationSyntax method in GetTestMethods(tree))
                {
                    int caseCount = CountAttributes(method.AttributeLists, "TestCase");

                    if (caseCount == 0)
                    {
                        continue;
                    }

                    Assert.That(
                        caseCount,
                        Is.GreaterThanOrEqualTo(2),
                        $"{source}: the parameterised test '{method.Identifier.ValueText}' must declare at least two cases.");
                }
            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenEveryTestAsserts()
        {
            foreach (string source in GetTestSourcePaths())
            {
                SyntaxTree tree = ParseTree(source);

                List<string> assertionFreeTests = [];

                foreach (MethodDeclarationSyntax method in GetTestMethods(tree))
                {
                    bool asserts = method.DescendantNodes()
                        .OfType<InvocationExpressionSyntax>()
                        .Any(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                            (memberAccess.Expression.ToString().StartsWith("Assert", StringComparison.Ordinal) ||
                                memberAccess.Name.Identifier.ValueText == "MarkupMatches"));

                    if (!asserts)
                    {
                        assertionFreeTests.Add(method.Identifier.ValueText);
                    }
                }

                Assert.That(
                    assertionFreeTests,
                    Is.Empty,
                    $"{source}: every test must assert -> {string.Join(", ", assertionFreeTests)}.");
            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenSetUpAndTearDownMethodsUseTheDocumentedNames()
        {
            foreach (string source in GetTestSourcePaths())
            {
                SyntaxTree tree = ParseTree(source);

                foreach (MethodDeclarationSyntax method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    if (HasAttribute(method.AttributeLists, "SetUp"))
                    {
                        Assert.That(method.Identifier.ValueText, Is.EqualTo("SetUp"), $"{source}: the [SetUp] method must be called SetUp.");
                    }

                    if (HasAttribute(method.AttributeLists, "TearDown"))
                    {
                        Assert.That(method.Identifier.ValueText, Is.EqualTo("TearDown"), $"{source}: the [TearDown] method must be called TearDown.");
                    }
                }
            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenNoCommentBannersGroupTests()
        {
            foreach (string source in GetTestSourcePaths())
            {
                foreach (string line in ReadSource(source).Split('\n'))
                {
                    string trimmed = line.Trim();

                    if (!trimmed.StartsWith("//", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Assert.That(
                        trimmed.TrimStart('/').Trim().Length,
                        Is.GreaterThan(0),
                        $"{source}: comment banners must not group tests.");
                }
            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenNoAlternativeTestFrameworkIsUsed()
        {
            string[] alternativeNamespaces = ["Xunit", "Microsoft.VisualStudio.TestTools.UnitTesting"];

            foreach (string source in GetTestSourcePaths())
            {
                SyntaxTree tree = ParseTree(source);

                bool importsAlternative = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<UsingDirectiveSyntax>()
                    .Select(usingDirective => usingDirective.Name?.ToString() ?? string.Empty)
                    .Any(nameSpace => alternativeNamespaces.Any(candidate =>
                        nameSpace == candidate || nameSpace.StartsWith(candidate + ".", StringComparison.Ordinal)));

                Assert.That(importsAlternative, Is.False, $"{source}: NUnit is the only permitted test framework.");
            }
        }

        private static IEnumerable<string> GetTestSourcePaths()
        {
            IReadOnlyList<string> sources = SourceSyntax.GetSources(ArchitectureResources.UnitTestSourcePrefix)
                .Select(source => ArchitectureResources.UnitTestSourcePrefix + source)
                .Concat(SourceSyntax.GetSources(ArchitectureResources.IntegrationTestSourcePrefix)
                    .Select(source => ArchitectureResources.IntegrationTestSourcePrefix + source))
                .ToArray();

            Assert.That(sources, Is.Not.Empty, "No test source files were found.");

            return sources;
        }

        private static string ReadSource(string path)
        {
            (string prefix, string relativePath) = SplitPath(path);

            return SourceSyntax.ReadSource(prefix, relativePath);
        }

        private static SyntaxTree ParseTree(string path)
        {
            (string prefix, string relativePath) = SplitPath(path);

            return SourceSyntax.ParseSource(prefix, relativePath);
        }

        private static (string Prefix, string RelativePath) SplitPath(string path)
        {
            if (path.StartsWith(ArchitectureResources.UnitTestSourcePrefix, StringComparison.Ordinal))
            {
                return (ArchitectureResources.UnitTestSourcePrefix, path.Substring(ArchitectureResources.UnitTestSourcePrefix.Length));
            }

            return (ArchitectureResources.IntegrationTestSourcePrefix, path.Substring(ArchitectureResources.IntegrationTestSourcePrefix.Length));
        }

        private static IEnumerable<MethodDeclarationSyntax> GetTestMethods(SyntaxTree tree)
            => tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(method => HasAttribute(method.AttributeLists, "Test") || HasAttribute(method.AttributeLists, "TestCase"));

        private static bool HasTests(ClassDeclarationSyntax fixture)
            => fixture.Members
                .OfType<MethodDeclarationSyntax>()
                .Any(method => HasAttribute(method.AttributeLists, "Test") || HasAttribute(method.AttributeLists, "TestCase"));

        private static bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
            => attributeLists
                .SelectMany(attributeList => attributeList.Attributes)
                .Any(attribute => string.Equals(attribute.Name.ToString(), attributeName, StringComparison.Ordinal));

        private static int CountAttributes(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
            => attributeLists
                .SelectMany(attributeList => attributeList.Attributes)
                .Count(attribute => string.Equals(attribute.Name.ToString(), attributeName, StringComparison.Ordinal));

        private static string GetFileStem(string path)
        {
            string fileName = path.Substring(path.LastIndexOf('/') + 1);

            return fileName.Substring(0, fileName.LastIndexOf('.'));
        }

        private static string GetExpectedNamespace(string path)
        {
            (string prefix, string relativePath) = SplitPath(path);

            string projectRootNamespace = prefix == ArchitectureResources.UnitTestSourcePrefix
                ? "NuciCraftInfo.UnitTests"
                : "NuciCraftInfo.IntegrationTests";

            int lastSeparatorIndex = relativePath.LastIndexOf('/');

            string folderPath = lastSeparatorIndex < 0
                ? string.Empty
                : relativePath.Substring(0, lastSeparatorIndex).Replace('/', '.');

            return string.IsNullOrEmpty(folderPath)
                ? projectRootNamespace
                : $"{projectRootNamespace}.{folderPath}";
        }
    }
}