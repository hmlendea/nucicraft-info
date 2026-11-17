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
    public sealed class MemberOrganisationTests
    {
        [Test]
        public void GivenProductionSources_WhenChecked_ThenMembersAreOrderedByKind()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                foreach (TypeDeclarationSyntax typeDeclaration in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    int[] kinds = typeDeclaration.Members
                        .Select(GetMemberKindOrder)
                        .Where(order => order >= 0)
                        .ToArray();

                    Assert.That(
                        kinds,
                        Is.Ordered,
                        $"{source}: members of '{typeDeclaration.Identifier.ValueText}' must be ordered fields -> properties -> events -> constructors -> methods.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenFieldOrderFollowsStaticReadonlyThenInstance()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                foreach (TypeDeclarationSyntax typeDeclaration in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    int[] orders = typeDeclaration.Members
                        .OfType<FieldDeclarationSyntax>()
                        .Select(GetFieldKindOrder)
                        .ToArray();

                    Assert.That(
                        orders,
                        Is.Ordered,
                        $"{source}: fields of '{typeDeclaration.Identifier.ValueText}' must be ordered static readonly -> static mutable -> instance readonly -> instance mutable.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenOverloadsAreAdjacentAndOrderedByParameterCount()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                foreach (TypeDeclarationSyntax typeDeclaration in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    IReadOnlyList<IGrouping<string, MethodDeclarationSyntax>> overloadGroups = typeDeclaration.Members
                        .OfType<MethodDeclarationSyntax>()
                        .GroupBy(method => method.Identifier.ValueText)
                        .Where(group => group.Count() > 1)
                        .ToArray();

                    foreach (IGrouping<string, MethodDeclarationSyntax> overloads in overloadGroups)
                    {
                        int[] parameterCounts = overloads
                            .Select(method => method.ParameterList.Parameters.Count)
                            .ToArray();

                        Assert.That(
                            parameterCounts,
                            Is.Ordered,
                            $"{source}: the overloads of '{overloads.Key}' must be adjacent and ordered by parameter count.");
                    }
                }
            }
        }

        private static int GetMemberKindOrder(MemberDeclarationSyntax member)
        {
            if (member is FieldDeclarationSyntax)
            {
                return 0;
            }

            if (member is PropertyDeclarationSyntax)
            {
                return 1;
            }

            if (member is EventFieldDeclarationSyntax)
            {
                return 2;
            }

            if (member is ConstructorDeclarationSyntax)
            {
                return 3;
            }

            if (member is MethodDeclarationSyntax)
            {
                return 4;
            }

            return -1;
        }

        private static int GetFieldKindOrder(FieldDeclarationSyntax field)
        {
            bool isStatic = SourceSyntax.HasModifier(field.Modifiers, SyntaxKind.StaticKeyword);
            bool isReadOnly = SourceSyntax.HasModifier(field.Modifiers, SyntaxKind.ReadOnlyKeyword);

            if (isStatic && isReadOnly)
            {
                return 0;
            }

            if (isStatic)
            {
                return 1;
            }

            if (isReadOnly)
            {
                return 2;
            }

            return 3;
        }
    }
}