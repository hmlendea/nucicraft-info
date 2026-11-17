using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

using NUnit.Framework;

namespace NuciCraftInfo.ArchitectureTests
{
    /// <summary>
    /// Enforces the symbol-level conventions that require semantic resolution: interface
    /// implementation alignment, parameter-name parity, and member-shape discipline.
    /// </summary>
    [TestFixture]
    public sealed class SemanticConventionTests
    {
        [Test]
        public void GivenImplementations_WhenResolved_ThenInterfaceAndImplementationParameterNamesMatch()
        {
            List<string> mismatches = [];

            foreach ((INamedTypeSymbol contract, INamedTypeSymbol implementation) in GetImplementations())
            {
                foreach (IMethodSymbol interfaceMethod in contract.GetMembers().OfType<IMethodSymbol>())
                {
                    IMethodSymbol? resolved = implementation.FindImplementationForInterfaceMember(interfaceMethod) as IMethodSymbol;

                    if (resolved is null ||
                        resolved.Parameters.Length != interfaceMethod.Parameters.Length)
                    {
                        continue;
                    }

                    for (int index = 0; index < interfaceMethod.Parameters.Length; index++)
                    {
                        string expected = interfaceMethod.Parameters[index].Name;
                        string actual = resolved.Parameters[index].Name;

                        if (!string.Equals(expected, actual, StringComparison.Ordinal))
                        {
                            mismatches.Add($"{implementation.Name}.{resolved.Name}: parameter '{actual}' must be named '{expected}'");
                        }
                    }
                }
            }

            Assert.That(
                mismatches,
                Is.Empty,
                $"Interface and implementation parameter names must match exactly -> {string.Join("; ", mismatches)}.");
        }

        [Test]
        public void GivenImplementations_WhenResolved_ThenEveryInterfaceMemberIsImplemented()
        {
            List<string> gaps = [];

            foreach ((INamedTypeSymbol contract, INamedTypeSymbol implementation) in GetImplementations())
            {
                foreach (ISymbol member in contract.GetMembers())
                {
                    if (member is not IMethodSymbol and not IPropertySymbol and not IEventSymbol)
                    {
                        continue;
                    }

                    if (member.IsImplicitlyDeclared)
                    {
                        continue;
                    }

                    if (implementation.FindImplementationForInterfaceMember(member) is null)
                    {
                        gaps.Add($"{implementation.Name} does not implement '{member.Name}'");
                    }
                }
            }

            Assert.That(gaps, Is.Empty, $"Every interface member must be implemented -> {string.Join("; ", gaps)}.");
        }

        [Test]
        public void GivenProductionTypes_WhenResolved_ThenNoMemberHidesAnInheritedMember()
        {
            List<string> hidingMembers = [];

            foreach (INamedTypeSymbol type in GetProductionTypes())
            {
                foreach (ISymbol member in type.GetMembers())
                {
                    if (member.IsImplicitlyDeclared || member.IsOverride || member.Name is ".ctor" or ".cctor")
                    {
                        continue;
                    }

                    bool hidesBase = type.BaseType is not null &&
                        type.BaseType.GetMembers(member.Name).Any(baseMember => baseMember.Kind == member.Kind);

                    if (hidesBase)
                    {
                        hidingMembers.Add($"{type.Name}.{member.Name}");
                    }
                }
            }

            Assert.That(
                hidingMembers,
                Is.Empty,
                $"Members must not hide inherited members -> {string.Join(", ", hidingMembers)}.");
        }

        [Test]
        public void GivenProductionTypes_WhenResolved_ThenNoPublicFieldIsDeclared()
        {
            List<string> publicFields = [];

            foreach (INamedTypeSymbol type in GetProductionTypes())
            {
                foreach (IFieldSymbol field in type.GetMembers().OfType<IFieldSymbol>())
                {
                    if (field.DeclaredAccessibility == Accessibility.Public && !field.IsImplicitlyDeclared)
                    {
                        publicFields.Add($"{type.Name}.{field.Name}");
                    }
                }
            }

            Assert.That(publicFields, Is.Empty, $"Public fields must be replaced with properties -> {string.Join(", ", publicFields)}.");
        }

        [Test]
        public void GivenProductionTypes_WhenResolved_ThenNoMutableStaticStateIsDeclared()
        {
            List<string> mutableStatics = [];

            foreach (INamedTypeSymbol type in GetProductionTypes())
            {
                foreach (IFieldSymbol field in type.GetMembers().OfType<IFieldSymbol>())
                {
                    if (!field.IsStatic || field.IsReadOnly || field.IsConst || field.IsImplicitlyDeclared)
                    {
                        continue;
                    }

                    mutableStatics.Add($"{type.Name}.{field.Name}");
                }
            }

            Assert.That(
                mutableStatics,
                Is.Empty,
                $"Static fields must be readonly or constant -> {string.Join(", ", mutableStatics)}.");
        }

        [Test]
        public void GivenProductionTypes_WhenResolved_ThenNoTypeNestsAnotherType()
        {
            List<string> nestedTypes = [];

            foreach (INamedTypeSymbol type in GetProductionTypes())
            {
                foreach (INamedTypeSymbol nested in type.GetTypeMembers())
                {
                    nestedTypes.Add($"{type.Name}.{nested.Name}");
                }
            }

            Assert.That(nestedTypes, Is.Empty, $"Nested types must be extracted into their own files -> {string.Join(", ", nestedTypes)}.");
        }

        private static IEnumerable<(INamedTypeSymbol Contract, INamedTypeSymbol Implementation)> GetImplementations()
        {
            foreach (INamedTypeSymbol type in GetProductionTypes())
            {
                if (type.TypeKind != TypeKind.Class)
                {
                    continue;
                }

                foreach (INamedTypeSymbol contract in type.AllInterfaces)
                {
                    bool declaredInProduction = contract.DeclaringSyntaxReferences.Any(reference =>
                        reference.SyntaxTree is not null &&
                        SourceSyntax.IsProductionTree(reference.SyntaxTree));

                    if (!declaredInProduction)
                    {
                        continue;
                    }

                    yield return (contract, type);
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> GetProductionTypes()
            => SourceSyntax.GetDeclaredTypeSymbols()
                .Where(type => SourceSyntax.IsProductionTree(type.DeclaringSyntaxReferences[0].SyntaxTree));
    }
}
