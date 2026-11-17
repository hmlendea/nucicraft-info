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
    /// Enforces the shared test conventions across the unit and integration suites:
    /// configuration exclusion, method grouping, parameter limits, and portability.
    /// </summary>
    [TestFixture]
    public sealed class TestabilityConventionTests
    {
        private static string[] ConfigurationTypes =>
        [
            "IConfiguration",
            "IConfigurationRoot",
            "IConfigurationSection",
            "ConfigurationBuilder",
            "ConfigurationManager"
        ];

        [Test]
        public void GivenUnitTestSources_WhenChecked_ThenNoConfigurationTypesAreRead()
        {
            foreach (string source in SourceSyntax.GetSources(ArchitectureResources.UnitTestSourcePrefix))
            {
                string fullSource = ArchitectureResources.UnitTestSourcePrefix + source;
                SyntaxTree tree = SourceSyntax.ParseAnySource(fullSource);

                IEnumerable<string> configurationReads = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Select(identifier => identifier.Identifier.ValueText)
                    .Where(name => ConfigurationTypes.Contains(name, StringComparer.Ordinal))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                Assert.That(
                    configurationReads,
                    Is.Empty,
                    $"{fullSource}: configuration values belong to integration tests or runtime checks, never to unit tests.");
            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenTestMethodsAreGroupedByProductionMethod()
        {
            foreach (string source in SourceSyntax.GetTestSources())
            {
                SyntaxTree tree = SourceSyntax.ParseAnySource(source);

                foreach (ClassDeclarationSyntax fixture in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    IReadOnlyList<MethodDeclarationSyntax> testMethods = fixture.Members
                        .OfType<MethodDeclarationSyntax>()
                        .Where(method => SourceSyntax.HasAttribute(method.AttributeLists, "Test") ||
                            SourceSyntax.HasAttribute(method.AttributeLists, "TestCase"))
                        .ToArray();

                    if (testMethods.Count < 2)
                    {
                        continue;
                    }

                    IEnumerable<string> subjects = testMethods
                        .Select(method => GetThenSubject(method.Identifier.ValueText))
                        .Where(subject => subject.Length > 0);

                    Assert.That(
                        subjects.Any(),
                        Is.True,
                        $"{source}: the test methods of '{fixture.Identifier.ValueText}' must follow the Given/When/Then naming form.");
                }
            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenNoTypeExceedsSevenConstructorParameters()
        {
            foreach (string source in SourceSyntax.GetTestSources())
            {
                SyntaxTree tree = SourceSyntax.ParseAnySource(source);

                IEnumerable<ConstructorDeclarationSyntax> oversizedConstructors = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ConstructorDeclarationSyntax>()
                    .Where(constructor => constructor.ParameterList.Parameters.Count > 7)
                    .ToArray();

                Assert.That(
                    oversizedConstructors,
                    Is.Empty,
                    $"{source}: constructors must have no more than seven parameters.");
            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenTestsDoNotReflectOverNonPublicCode()
        {
            foreach (string source in SourceSyntax.GetTestSources())
            {
                string content = SourceSyntax.ReadAnySource(source);

                Assert.That(
                    content,
                    Does.Not.Contain("BindingFlags.NonPublic"),
                    $"{source}: tests must not use reflection to test non-public code.");

                Assert.That(
                    content,
                    Does.Not.Contain("GetField(\""),
                    $"{source}: tests must not use reflection to reach private state.");            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenFixtureClassesAreNotNested()
        {
            foreach (string source in SourceSyntax.GetTestSources())
            {
                SyntaxTree tree = SourceSyntax.ParseAnySource(source);

                IEnumerable<ClassDeclarationSyntax> nestedFixtures = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(declaration => declaration.Parent is TypeDeclarationSyntax)
                    .Where(declaration => SourceSyntax.HasAttribute(declaration.AttributeLists, "TestFixture"))
                    .ToArray();

                Assert.That(
                    nestedFixtures,
                    Is.Empty,
                    $"{source}: test fixtures must be top-level classes, one per file.");
            }
        }

        [Test]
        public void GivenTestSources_WhenChecked_ThenEveryTestDeclaresATestAttribute()
        {
            foreach (string source in SourceSyntax.GetTestSources())
            {
                SyntaxTree tree = SourceSyntax.ParseAnySource(source);

                IEnumerable<MethodDeclarationSyntax> unannotatedPublicMethods = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => method.Parent is ClassDeclarationSyntax fixture &&
                        SourceSyntax.HasAttribute(fixture.AttributeLists, "TestFixture"))
                    .Where(method => SourceSyntax.HasModifier(method.Modifiers, SyntaxKind.PublicKeyword))
                    .Where(method => !SourceSyntax.HasAttribute(method.AttributeLists, "Test"))
                    .Where(method => !SourceSyntax.HasAttribute(method.AttributeLists, "TestCase"))
                    .Where(method => !SourceSyntax.HasAttribute(method.AttributeLists, "TestCaseSource"))
                    .Where(method => !SourceSyntax.HasAttribute(method.AttributeLists, "SetUp"))
                    .Where(method => !SourceSyntax.HasAttribute(method.AttributeLists, "TearDown"))
                    .Where(method => !SourceSyntax.HasAttribute(method.AttributeLists, "OneTimeSetUp"))
                    .Where(method => !SourceSyntax.HasAttribute(method.AttributeLists, "OneTimeTearDown"))
                    .ToArray();

                Assert.That(
                    unannotatedPublicMethods,
                    Is.Empty,
                    $"{source}: every public fixture method must declare a NUnit test or lifecycle attribute.");
            }
        }

        private static string GetThenSubject(string methodName)
        {
            int thenIndex = methodName.IndexOf("_Then", StringComparison.Ordinal);

            if (thenIndex < 0)
            {
                return string.Empty;
            }

            return methodName.Substring(thenIndex + "_Then".Length);
        }
    }
}
