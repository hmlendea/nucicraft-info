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
    /// Enforces the naming conventions that apply to data and domain types, including the
    /// role-specific suffixes and the colocation of interfaces with their implementations.
    /// </summary>
    [TestFixture]
    public sealed class TypeNamingConventionTests
    {
        private static string[] ReservedTypeSuffixes =>
        [
            "Request",
            "Response",
            "Settings",
            "DataObject",
            "MappingExtensions",
            "Tests",
            "Extensions",
            "Attribute",
            "Exception"
        ];

        [Test]
        public void GivenResponseTypes_WhenChecked_ThenTheyUseTheGetNounResponseForm()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                string[] invalidNames = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BaseTypeDeclarationSyntax>()
                    .Where(declaration => declaration.Identifier.ValueText.EndsWith("Response", StringComparison.Ordinal))
                    .Select(declaration => declaration.Identifier.ValueText)
                    .Where(name => !name.StartsWith("Get", StringComparison.Ordinal))
                    .ToArray();

                Assert.That(
                    invalidNames,
                    Is.Empty,
                    $"{source}: response DTOs must use the 'Get' + Noun + 'Response' form -> {string.Join(", ", invalidNames)}.");
            }
        }

        [Test]
        public void GivenRequestTypes_WhenChecked_ThenTheyUseTheVerbNounRequestForm()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                string[] invalidNames = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BaseTypeDeclarationSyntax>()
                    .Where(declaration => declaration.Identifier.ValueText.EndsWith("Request", StringComparison.Ordinal))
                    .Select(declaration => declaration.Identifier.ValueText)
                    .Where(name => name.Length <= "Request".Length)
                    .ToArray();

                Assert.That(
                    invalidNames,
                    Is.Empty,
                    $"{source}: request DTOs must use the Verb + Noun + 'Request' form -> {string.Join(", ", invalidNames)}.");
            }
        }

        [Test]
        public void GivenDomainModels_WhenChecked_ThenTheyCarryNoReservedRoleSuffix()
        {
            string[] modelNamespaces = ["NuciCraftInfo.Requests", "NuciCraftInfo.Responses"];

            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                string nameSpace = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BaseNamespaceDeclarationSyntax>()
                    .Select(declaration => declaration.Name.ToString())
                    .FirstOrDefault() ?? string.Empty;

                if (modelNamespaces.Contains(nameSpace, StringComparer.Ordinal))
                {
                    continue;
                }

                string[] suffixedModels = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Select(declaration => declaration.Identifier.ValueText)
                    .Where(name => name.EndsWith("DataObject", StringComparison.Ordinal))
                    .ToArray();

                Assert.That(
                    suffixedModels,
                    Is.Empty,
                    $"{source}: the 'DataObject' suffix belongs only to persistence data objects.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenContractsUseTheSettingsSuffix()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                string[] invalidNames = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Select(declaration => declaration.Identifier.ValueText)
                    .Where(name => name.EndsWith("Configuration", StringComparison.Ordinal) ||
                        name.EndsWith("Config", StringComparison.Ordinal))
                    .Where(name => !name.EndsWith("Settings", StringComparison.Ordinal))
                    .ToArray();

                Assert.That(
                    invalidNames,
                    Is.Empty,
                    $"{source}: configuration classes must use the Noun + 'Settings' form -> {string.Join(", ", invalidNames)}.");
            }
        }

        [Test]
        public void GivenStandardRoleNamespaces_WhenChecked_ThenTheirTypesUseTheDocumentedNames()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                string nameSpace = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BaseNamespaceDeclarationSyntax>()
                    .Select(declaration => declaration.Name.ToString())
                    .FirstOrDefault() ?? string.Empty;

                Assert.That(
                    nameSpace,
                    Does.Not.EndWith(".Enumerations"),
                    $"{source}: enumerations must be colocated with their domain models, never placed in an '.Enumerations' namespace.");
                Assert.That(
                    nameSpace,
                    Does.Not.EndWith(".Enums"),
                    $"{source}: enumerations must be colocated with their domain models, never placed in an '.Enums' namespace.");
                Assert.That(
                    nameSpace,
                    Does.Not.EndWith(".Interfaces"),
                    $"{source}: interfaces must be colocated with their implementations, never placed in an '.Interfaces' namespace.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoCatchAllUtilityNamespacesAreDeclared()
        {
            string[] forbiddenSegments = ["Helpers", "Utils", "Utilities", "Common", "Misc", "Shared"];

            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                string[] forbiddenNamespaces = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BaseNamespaceDeclarationSyntax>()
                    .Select(declaration => declaration.Name.ToString())
                    .Where(nameSpace => nameSpace
                        .Split('.')
                        .Any(segment => forbiddenSegments.Contains(segment, StringComparer.Ordinal)))
                    .ToArray();

                Assert.That(
                    forbiddenNamespaces,
                    Is.Empty,
                    $"{source}: catch-all helper, utility, common, misc, and shared namespaces are not allowed.");
            }
        }

        [Test]
        public void GivenMappingExtensions_WhenChecked_ThenTheyUseTheMappingExtensionsSuffix()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                string[] invalidNames = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(declaration => declaration.Members
                        .OfType<MethodDeclarationSyntax>()
                        .Any(method => method.Identifier.ValueText.StartsWith("To", StringComparison.Ordinal) &&
                            method.ParameterList.Parameters.Count > 0 &&
                            method.ParameterList.Parameters[0].Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ThisKeyword))))
                    .Select(declaration => declaration.Identifier.ValueText)
                    .Where(name => !name.EndsWith("MappingExtensions", StringComparison.Ordinal))
                    .ToArray();

                Assert.That(
                    invalidNames,
                    Is.Empty,
                    $"{source}: mapping extension classes must be named Noun + 'MappingExtensions' -> {string.Join(", ", invalidNames)}.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenTypeSuffixesAreRecognisedRoles()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                string[] suspiciousNames = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Select(declaration => declaration.Identifier.ValueText)
                    .Where(name => name.EndsWith("Class", StringComparison.Ordinal) ||
                        name.EndsWith("Object", StringComparison.Ordinal) ||
                        name.EndsWith("Impl", StringComparison.Ordinal))
                    .Where(name => !ReservedTypeSuffixes.Any(suffix => name.EndsWith(suffix, StringComparison.Ordinal)))
                    .ToArray();

                Assert.That(
                    suspiciousNames,
                    Is.Empty,
                    $"{source}: types must not use redundant 'Class', 'Object', or 'Impl' suffixes -> {string.Join(", ", suspiciousNames)}.");
            }
        }
    }
}
