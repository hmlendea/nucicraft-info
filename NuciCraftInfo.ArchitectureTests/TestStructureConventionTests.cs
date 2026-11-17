using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace NuciCraftInfo.ArchitectureTests
{
    /// <summary>
    /// Enforces the shared and unit-level testing conventions across every test project,
    /// including helper placement, dummy types, and the NUnit assertion model.
    /// </summary>
    [TestFixture]
    public sealed class TestStructureConventionTests
    {
        private static string[] ForbiddenAssertions =>
        [
            "Assert.AreEqual",
            "Assert.IsTrue",
            "Assert.IsFalse",
            "Assert.IsNull",
            "Assert.IsNotNull",
            "Assert.Greater",
            "Assert.Less",
            "Assert.Zero",
            "Assert.NotZero"
        ];

        [Test]
        public void GivenTestSources_WhenChecked_ThenTheNUnitConstraintModelIsUsed()
        {
            foreach (string source in SourceSyntax.GetTestSources())
            {
                string content = SourceSyntax.ReadAnySource(source);

                foreach (string forbiddenAssertion in ForbiddenAssertions)
                {
                    Assert.That(
                        content,
                        Does.Not.Contain(forbiddenAssertion + "("),
                        $"{source}: use Assert.That with NUnit constraints instead of {forbiddenAssertion}.");
                }            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenPrivateStaticHelpersArePlacedAtTheClassBottom()
        {
            foreach (string source in SourceSyntax.GetTestSources())
            {
                SyntaxTree tree = SourceSyntax.ParseAnySource(source);

                foreach (ClassDeclarationSyntax fixture in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    IReadOnlyList<MemberDeclarationSyntax> members = fixture.Members.ToArray();
                    int firstHelperIndex = -1;
                    int lastNonHelperIndex = -1;

                    for (int index = 0; index < members.Count; index++)
                    {
                        bool isHelper = members[index] is MethodDeclarationSyntax method &&
                            SourceSyntax.HasModifier(method.Modifiers, SyntaxKind.PrivateKeyword);

                        if (isHelper)
                        {
                            if (firstHelperIndex < 0)
                            {
                                firstHelperIndex = index;
                            }

                            continue;
                        }

                        lastNonHelperIndex = index;
                    }

                    if (firstHelperIndex < 0)
                    {
                        continue;
                    }

                    Assert.That(
                        firstHelperIndex > lastNonHelperIndex,
                        Is.True,
                        $"{source}: the private static helpers of '{fixture.Identifier.ValueText}' must be placed at the class bottom.");
                }
            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenStateFreeHelpersAreStatic()
        {
            foreach (string source in SourceSyntax.GetTestSources())
            {
                SyntaxTree tree = SourceSyntax.ParseAnySource(source);

                IEnumerable<MethodDeclarationSyntax> instanceHelpers = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => SourceSyntax.HasModifier(method.Modifiers, SyntaxKind.PrivateKeyword))
                    .Where(method => !SourceSyntax.HasModifier(method.Modifiers, SyntaxKind.StaticKeyword))
                    .Where(method => !SourceSyntax.HasAttribute(method.AttributeLists, "SetUp"))
                    .Where(method => !SourceSyntax.HasAttribute(method.AttributeLists, "TearDown"))
                    .Where(method => method.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .All(identifier => !RefersToInstanceField(identifier, method)))
                    .ToArray();

                Assert.That(
                    instanceHelpers,
                    Is.Empty,
                    $"{source}: private helpers without instance state must be declared static.");
            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenDummyImplementationsAreTopLevelInternalTypes()
        {
            foreach (string source in SourceSyntax.GetTestSources())
            {
                SyntaxTree tree = SourceSyntax.ParseAnySource(source);

                IEnumerable<ClassDeclarationSyntax> nestedDummies = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(declaration => declaration.Parent is TypeDeclarationSyntax)
                    .Where(declaration => declaration.Identifier.ValueText.StartsWith("Stub", StringComparison.Ordinal) ||
                        declaration.Identifier.ValueText.StartsWith("Dummy", StringComparison.Ordinal) ||
                        declaration.Identifier.ValueText.StartsWith("Fake", StringComparison.Ordinal))
                    .ToArray();

                Assert.That(
                    nestedDummies,
                    Is.Empty,
                    $"{source}: dummy implementations must be top-level internal classes in separate files.");
            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenDummyTypesAreInternal()
        {
            foreach (string source in SourceSyntax.GetTestSources())
            {
                SyntaxTree tree = SourceSyntax.ParseAnySource(source);

                IEnumerable<string> publicDummies = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(declaration => declaration.Identifier.ValueText.StartsWith("Stub", StringComparison.Ordinal) ||
                        declaration.Identifier.ValueText.StartsWith("Dummy", StringComparison.Ordinal) ||
                        declaration.Identifier.ValueText.StartsWith("Fake", StringComparison.Ordinal))
                    .Where(declaration => SourceSyntax.HasModifier(declaration.Modifiers, SyntaxKind.PublicKeyword))
                    .Select(declaration => declaration.Identifier.ValueText)
                    .ToArray();

                Assert.That(
                    publicDummies,
                    Is.Empty,
                    $"{source}: dummy custom implementations must be declared internal.");
            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenJsonSerializerOptionsAreCached()
        {
            foreach (string source in SourceSyntax.GetTestSources())
            {
                SyntaxTree tree = SourceSyntax.ParseAnySource(source);

                IEnumerable<ObjectCreationExpressionSyntax> volatileOptions = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ObjectCreationExpressionSyntax>()
                    .Where(creation => creation.Type.ToString().Contains("JsonSerializerOptions", StringComparison.Ordinal))
                    .Where(creation => creation.Ancestors().OfType<FieldDeclarationSyntax>().All(field =>
                        !SourceSyntax.HasModifier(field.Modifiers, SyntaxKind.StaticKeyword) ||
                        !SourceSyntax.HasModifier(field.Modifiers, SyntaxKind.ReadOnlyKeyword)))
                    .ToArray();

                Assert.That(
                    volatileOptions,
                    Is.Empty,
                    $"{source}: JsonSerializerOptions must be cached in static readonly fields, never constructed per test.");
            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenHandwrittenPathsAndCultureDependentOperationsAreAbsent()
        {
            foreach (string source in SourceSyntax.GetTestSources())
            {
                string content = SourceSyntax.ReadAnySource(source);

                Assert.That(
                    content,
                    Does.Not.Match(@"[""'][A-Za-z]:\\"),
                    $"{source}: hard-coded Windows paths make the tests machine-dependent.");

                Assert.That(
                    content,
                    Does.Not.Match(@"\bCultureInfo\.CurrentCulture\b"),
                    $"{source}: tests must not depend on the current culture.");

                Assert.That(
                    content,
                    Does.Not.Match(@"\bDateTime\.Now\b|\bDateTime\.UtcNow\b|\bEnvironment\.MachineName\b|\bEnvironment\.UserName\b|\bEnvironment\.CurrentDirectory\b"),
                    $"{source}: tests must not depend on the clock, machine name, user name, or current directory.");
            }
        }

        private static bool RefersToInstanceField(IdentifierNameSyntax identifier, MethodDeclarationSyntax method)
        {
            if (method.Parent is not TypeDeclarationSyntax declaration)
            {
                return false;
            }

            return declaration.Members
                .OfType<FieldDeclarationSyntax>()
                .Any(field => field.Declaration.Variables
                    .Any(variable => string.Equals(variable.Identifier.ValueText, identifier.Identifier.ValueText, StringComparison.Ordinal)));
        }
    }
}
