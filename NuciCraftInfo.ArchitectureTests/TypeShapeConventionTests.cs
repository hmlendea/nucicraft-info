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
    /// Enforces the type-declaration, construction, and contract conventions that the
    /// coding instructions define for classes, interfaces, and exception types.
    /// </summary>
    [TestFixture]
    public sealed class TypeShapeConventionTests
    {
        private static string[] ContractSuffixes => ["Request", "Response", "Settings"];

        private static string[] ModelExemptSuffixes => ["DataObject", "Request", "Response", "Settings", "Extensions"];

        [Test]
        public void GivenProductionSources_WhenChecked_ThenAutoPropertiesAreUsedForContractsAndModels()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<PropertyDeclarationSyntax> computedDataProperties = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .Where(property => IsContractType(property.Parent as TypeDeclarationSyntax))
                    .Where(property => property.AccessorList is not null)
                    .Where(property => property.AccessorList!.Accessors.Any(accessor => accessor.Body is not null))
                    .ToArray();

                Assert.That(
                    computedDataProperties,
                    Is.Empty,
                    $"{source}: request, response, and settings contracts must expose auto-properties.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoInitOnlyAccessorsAreDeclared()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<AccessorDeclarationSyntax> initAccessors = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<AccessorDeclarationSyntax>()
                    .Where(accessor => accessor.IsKind(SyntaxKind.InitAccessorDeclaration))
                    .ToArray();

                Assert.That(initAccessors, Is.Empty, $"{source}: init-only accessors are not allowed.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenServicesAndCompositionRootsUsePrimaryConstructors()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<ClassDeclarationSyntax> fieldBackedServices = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(declaration => declaration.Identifier.ValueText.EndsWith("Service", StringComparison.Ordinal))
                    .Where(declaration => !declaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)))
                    .Where(declaration => declaration.ParameterList is null)
                    .Where(declaration => declaration.Members.OfType<FieldDeclarationSyntax>().Any(field =>
                        !SourceSyntax.HasModifier(field.Modifiers, SyntaxKind.StaticKeyword)))
                    .ToArray();

                Assert.That(
                    fieldBackedServices,
                    Is.Empty,
                    $"{source}: services must use primary constructors and their parameters directly.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenExplicitAccessibilityIsDeclaredOnTypesAndMembers()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);
                SyntaxNode root = tree.GetRoot();

                IEnumerable<MemberDeclarationSyntax> undeclaredMembers = root
                    .DescendantNodes()
                    .OfType<MemberDeclarationSyntax>()
                    .Where(member => member is BaseTypeDeclarationSyntax ||
                        member is BaseFieldDeclarationSyntax ||
                        member is BasePropertyDeclarationSyntax ||
                        member is BaseMethodDeclarationSyntax ||
                        member is DelegateDeclarationSyntax)
                    .Where(member => !SourceSyntax.DeclaresAccessibility(SourceSyntax.GetModifiers(member)))
                    .ToArray();

                Assert.That(undeclaredMembers, Is.Empty, $"{source}: every type and member must declare an explicit accessibility.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenClassesWithPublicMethodsDeclareAColocatedInterface()
        {
            IReadOnlyList<SyntaxTree> productionTrees = SourceSyntax.GetProductionSources()
                .Select(SourceSyntax.ParseProductionSource)
                .ToArray();

            string[] declaredInterfaces = productionTrees
                .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>())
                .Select(declaration => declaration.Identifier.ValueText)
                .ToArray();

            Assert.That(declaredInterfaces, Is.Not.Empty, "The production sources must declare at least one interface.");

            List<string> unpairedClasses = [];

            foreach (SyntaxTree tree in productionTrees)
            {
                foreach (ClassDeclarationSyntax declaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    if (declaration.Modifiers.Any(modifier =>
                        modifier.IsKind(SyntaxKind.StaticKeyword) ||
                        modifier.IsKind(SyntaxKind.AbstractKeyword)))
                    {
                        continue;
                    }

                    if (ModelExemptSuffixes.Any(suffix => declaration.Identifier.ValueText.EndsWith(suffix, StringComparison.Ordinal)))
                    {
                        continue;
                    }

                    bool hasPublicMethod = declaration.Members
                        .OfType<MethodDeclarationSyntax>()
                        .Any(method => SourceSyntax.HasModifier(method.Modifiers, SyntaxKind.PublicKeyword));

                    if (!hasPublicMethod)
                    {
                        continue;
                    }

                    bool implementsInterface = declaration.BaseList is not null &&
                        declaration.BaseList.Types.Any(baseType =>
                            declaredInterfaces.Contains(baseType.Type.ToString(), StringComparer.Ordinal));

                    if (!implementsInterface)
                    {
                        unpairedClasses.Add(declaration.Identifier.ValueText);
                    }
                }
            }

            Assert.That(
                unpairedClasses,
                Is.Empty,
                $"Classes exposing public methods must implement a colocated interface -> {string.Join(", ", unpairedClasses)}.");
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoMarkerInterfacesAreDeclared()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<InterfaceDeclarationSyntax> markerInterfaces = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<InterfaceDeclarationSyntax>()
                    .Where(declaration => declaration.Members.Count == 0)
                    .ToArray();

                Assert.That(markerInterfaces, Is.Empty, $"{source}: marker interfaces without members are not allowed.");
            }
        }

        [Test]
        public void GivenCustomExceptionTypes_WhenChecked_ThenTheyUseTheExceptionSuffixAndForwardToTheBaseConstructor()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                foreach (ClassDeclarationSyntax declaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    bool inheritsException = declaration.BaseList is not null &&
                        declaration.BaseList.Types.Any(baseType =>
                            baseType.Type.ToString().EndsWith("Exception", StringComparison.Ordinal));

                    if (!inheritsException)
                    {
                        continue;
                    }

                    Assert.That(
                        declaration.Identifier.ValueText,
                        Does.EndWith("Exception"),
                        $"{source}: custom exception type names must end with 'Exception'.");

                    foreach (ConstructorDeclarationSyntax constructor in declaration.Members.OfType<ConstructorDeclarationSyntax>())
                    {
                        Assert.That(
                            constructor.Initializer,
                            Is.Not.Null,
                            $"{source}: exception constructors must forward to the base Exception constructor.");

                        bool forwardsInnerException = constructor.ParameterList.Parameters
                            .Any(parameter => string.Equals(parameter.Type?.ToString(), "Exception", StringComparison.Ordinal));

                        if (forwardsInnerException)
                        {
                            Assert.That(
                                constructor.Initializer!.ArgumentList.Arguments.Count,
                                Is.GreaterThanOrEqualTo(2),
                                $"{source}: constructors accepting an inner exception must forward it to the base constructor.");
                        }
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenTargetTypedConstructionIsUsedWhereTheTypeIsDeclared()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<ObjectCreationExpressionSyntax> redundantTypes = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ObjectCreationExpressionSyntax>()
                    .Where(creation => creation.Parent is EqualsValueClauseSyntax)
                    .Where(creation => creation.Parent!.Parent is VariableDeclaratorSyntax)
                    .Where(creation => creation.Parent!.Parent!.Parent is VariableDeclarationSyntax declaration &&
                        string.Equals(declaration.Type.ToString(), creation.Type.ToString(), StringComparison.Ordinal))
                    .ToArray();

                Assert.That(
                    redundantTypes,
                    Is.Empty,
                    $"{source}: use target-typed 'new(...)' when the declaration already states the type.");
            }
        }

        private static bool IsContractType(TypeDeclarationSyntax? declaration)
            => declaration is not null &&
                ContractSuffixes.Any(suffix => declaration.Identifier.ValueText.EndsWith(suffix, StringComparison.Ordinal));
    }
}