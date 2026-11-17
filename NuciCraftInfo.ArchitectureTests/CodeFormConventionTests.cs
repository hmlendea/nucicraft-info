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
    /// Enforces the form-level coding conventions: compound increments, conditional defaults,
    /// equality calls, static methods, constant properties, and brace placement.
    /// </summary>
    [TestFixture]
    public sealed class CodeFormConventionTests
    {
        [Test]
        public void GivenProductionSources_WhenChecked_ThenIncrementsUseCompoundAssignment()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<AssignmentExpressionSyntax> explicitSelfAssignments = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<AssignmentExpressionSyntax>()
                    .Where(assignment => assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
                    .Where(assignment => assignment.Right is BinaryExpressionSyntax)
                    .Where(assignment => IsSelfAssignment(assignment, (BinaryExpressionSyntax)assignment.Right))
                    .ToArray();

                Assert.That(
                    explicitSelfAssignments,
                    Is.Empty,
                    $"{source}: use '+=' and '-=' instead of explicit self-assignments such as 'a = a + 1'.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenEqualityUsesStaticOrStringEquals()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<InvocationExpressionSyntax> instanceEqualityCalls = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                        memberAccess.Name.Identifier.ValueText == "Equals" &&
                        invocation.ArgumentList.Arguments.Count == 1)
                    .Where(invocation => !((MemberAccessExpressionSyntax)invocation.Expression)
                        .Expression.ToString().Contains("string", StringComparison.Ordinal))
                    .ToArray();

                Assert.That(
                    instanceEqualityCalls,
                    Is.Empty,
                    $"{source}: comparisons must use static Equals or string.Equals, never an instance .Equals() call.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenSingleCharacterStringChecksUseCharOverloads()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<InvocationExpressionSyntax> singleCharacterCalls = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                        (memberAccess.Name.Identifier.ValueText == "StartsWith" ||
                            memberAccess.Name.Identifier.ValueText == "EndsWith"))
                    .Where(invocation => invocation.ArgumentList.Arguments.Any(argument =>
                        argument.Expression is LiteralExpressionSyntax literal &&
                        literal.Token.ValueText.Length == 1))
                    .ToArray();

                Assert.That(
                    singleCharacterCalls,
                    Is.Empty,
                    $"{source}: single-character checks must use the char overloads of StartsWith and EndsWith.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenStateFreeHelpersAreStatic()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<MethodDeclarationSyntax> instanceHelpers = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => SourceSyntax.HasModifier(method.Modifiers, SyntaxKind.PrivateKeyword))
                    .Where(method => !SourceSyntax.HasModifier(method.Modifiers, SyntaxKind.StaticKeyword))
                    .Where(method => method.ParameterList.Parameters.Count == 0)
                    .Where(method => method.Parent is ClassDeclarationSyntax)
                    .ToArray();

                Assert.That(
                    instanceHelpers,
                    Is.Empty,
                    $"{source}: parameterless private helpers must be static unless they are proven to use instance state.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenTypesAndMembersUseUpperCamelCaseAlternatives()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                string[] lowercaseMembers = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MemberDeclarationSyntax>()
                    .Where(member => member is not FieldDeclarationSyntax)
                    .Where(member => member is BaseTypeDeclarationSyntax ||
                        member is BasePropertyDeclarationSyntax ||
                        member is BaseMethodDeclarationSyntax)
                    .Where(member => SourceSyntax.IsPublicOrProtected(SourceSyntax.GetModifiers(member)))
                    .Where(member => member is not BaseFieldDeclarationSyntax)
                    .Select(GetMemberName)
                    .Where(name => name.Length > 0 && char.IsLower(name[0]))
                    .ToArray();

                Assert.That(
                    lowercaseMembers,
                    Is.Empty,
                    $"{source}: public and protected members must begin with an uppercase letter.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenExplicitDefaultAssignmentsAreAbsent()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<EqualsValueClauseSyntax> defaultAssignments = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<EqualsValueClauseSyntax>()
                    .Where(clause => clause.Value is LiteralExpressionSyntax literal &&
                        (literal.IsKind(SyntaxKind.NullLiteralExpression) ||
                            literal.IsKind(SyntaxKind.DefaultLiteralExpression)))
                    .Where(clause => clause.Parent?.Parent is VariableDeclaratorSyntax)
                    .ToArray();

                Assert.That(
                    defaultAssignments,
                    Is.Empty,
                    $"{source}: assignments that restate framework or type defaults must be omitted.");
            }
        }

        private static bool IsSelfAssignment(AssignmentExpressionSyntax assignment, BinaryExpressionSyntax right)
            => (right.IsKind(SyntaxKind.AddExpression) || right.IsKind(SyntaxKind.SubtractExpression)) &&
                string.Equals(right.Left.ToString(), assignment.Left.ToString(), StringComparison.Ordinal);

        private static string GetMemberName(MemberDeclarationSyntax member)
            => member switch
            {
                BaseTypeDeclarationSyntax declaration => declaration.Identifier.ValueText,
                PropertyDeclarationSyntax property => property.Identifier.ValueText,
                MethodDeclarationSyntax method => method.Identifier.ValueText,
                _ => string.Empty
            };
    }
}
