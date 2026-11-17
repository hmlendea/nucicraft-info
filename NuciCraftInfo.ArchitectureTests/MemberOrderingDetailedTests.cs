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
    public sealed class MemberOrderingDetailedTests
    {
        [Test]
        public void GivenProductionSources_WhenChecked_ThenEachMemberGroupIsOrderedByAccessibility()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                foreach (TypeDeclarationSyntax typeDeclaration in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    IReadOnlyList<MemberDeclarationSyntax> members = typeDeclaration.Members.ToArray();

                    int groupStart = 0;

                    while (groupStart < members.Count)
                    {
                        int groupKind = GetMemberKindOrder(members[groupStart]);

                        if (groupKind < 0)
                        {
                            groupStart++;
                            continue;
                        }

                        int groupEnd = groupStart + 1;

                        while (groupEnd < members.Count &&
                            GetMemberKindOrder(members[groupEnd]) == groupKind)
                        {
                            groupEnd++;
                        }

                        int[] accessibilities = members
                            .Skip(groupStart)
                            .Take(groupEnd - groupStart)
                            .Select(GetAccessibilityOrder)
                            .ToArray();

                        Assert.That(
                            accessibilities,
                            Is.Ordered,
                            $"{source}: members of kind {groupKind} in '{typeDeclaration.Identifier.ValueText}' must be ordered by accessibility.");

                        groupStart = groupEnd;
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenFieldsAreOrderedByAccessibilityWithinEachKind()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                foreach (TypeDeclarationSyntax typeDeclaration in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    IReadOnlyList<FieldDeclarationSyntax> fields = typeDeclaration.Members.OfType<FieldDeclarationSyntax>().ToArray();

                    int groupStart = 0;

                    while (groupStart < fields.Count)
                    {
                        int groupFlags = GetFieldKindFlags(fields[groupStart]);

                        if (groupFlags < 0)
                        {
                            groupStart++;
                            continue;
                        }

                        int groupEnd = groupStart + 1;

                        while (groupEnd < fields.Count &&
                            GetFieldKindFlags(fields[groupEnd]) == groupFlags)
                        {
                            groupEnd++;
                        }

                        int[] accessibilities = fields
                            .Skip(groupStart)
                            .Take(groupEnd - groupStart)
                            .Select(GetAccessibilityOrder)
                            .ToArray();

                        Assert.That(
                            accessibilities,
                            Is.Ordered,
                            $"{source}: fields of kind {groupFlags} in '{typeDeclaration.Identifier.ValueText}' must be ordered by accessibility.");

                        groupStart = groupEnd;
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

        private static int GetAccessibilityOrder(MemberDeclarationSyntax member)
        {
            SyntaxTokenList modifiers = GetModifiers(member);

            if (SourceSyntax.HasModifier(modifiers, SyntaxKind.PublicKeyword))
            {
                return 0;
            }

            if (SourceSyntax.HasModifier(modifiers, SyntaxKind.ProtectedKeyword))
            {
                return 1;
            }

            if (SourceSyntax.HasModifier(modifiers, SyntaxKind.InternalKeyword))
            {
                return 2;
            }

            return 3;
        }

        private static int GetFieldKindFlags(FieldDeclarationSyntax field)
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

        private static SyntaxTokenList GetModifiers(MemberDeclarationSyntax member)
            => member switch
            {
                FieldDeclarationSyntax field => field.Modifiers,
                PropertyDeclarationSyntax property => property.Modifiers,
                EventFieldDeclarationSyntax eventField => eventField.Modifiers,
                ConstructorDeclarationSyntax constructor => constructor.Modifiers,
                MethodDeclarationSyntax method => method.Modifiers,
                _ => default
            };
    }
}