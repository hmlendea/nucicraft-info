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
    /// Enforces the null-handling, exception, and casting conventions.
    /// </summary>
    [TestFixture]
    public sealed class ErrorHandlingConventionTests
    {
        [Test]
        public void GivenProductionSources_WhenChecked_ThenNullChecksUseIsNullPatterns()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<BinaryExpressionSyntax> equalityChecks = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BinaryExpressionSyntax>()
                    .Where(expression => expression.IsKind(SyntaxKind.EqualsExpression) ||
                        expression.IsKind(SyntaxKind.NotEqualsExpression))
                    .Where(expression => IsNullLiteral(expression.Right) || IsNullLiteral(expression.Left))
                    .ToArray();

                Assert.That(
                    equalityChecks,
                    Is.Empty,
                    $"{source}: null checks must use 'is null' or 'is not null', never '== null' or '!= null'.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoNullConditionalCoalescingIsUsed()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<AssignmentExpressionSyntax> coalescingAssignments = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<AssignmentExpressionSyntax>()
                    .Where(assignment => assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression))
                    .ToArray();

                IEnumerable<BinaryExpressionSyntax> coalescingExpressions = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BinaryExpressionSyntax>()
                    .Where(expression => expression.IsKind(SyntaxKind.CoalesceExpression))
                    .ToArray();

                Assert.That(coalescingExpressions, Is.Empty, $"{source}: the null-coalescing operator '??' is not allowed.");
                Assert.That(coalescingAssignments, Is.Empty, $"{source}: the null-coalescing assignment '??=' is not allowed.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenOnlyBclExceptionsAreThrown()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<TypeSyntax> thrownTypes = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ThrowStatementSyntax>()
                    .Select(throwStatement => throwStatement.Expression)
                    .OfType<ObjectCreationExpressionSyntax>()
                    .Select(creation => creation.Type)
                    .ToArray();

                string[] customExceptions = thrownTypes
                    .Select(type => type.ToString())
                    .Where(typeName => typeName.EndsWith("Exception", StringComparison.Ordinal))
                    .Where(typeName => !IsBclException(typeName))
                    .ToArray();

                Assert.That(
                    customExceptions,
                    Is.Empty,
                    $"{source}: only base class library exceptions may be thrown -> {string.Join(", ", customExceptions)}.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoCustomExceptionTypesAreDeclared()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<ClassDeclarationSyntax> customExceptions = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(declaration => declaration.BaseList is not null &&
                        declaration.BaseList.Types.Any(baseType =>
                            baseType.Type.ToString().EndsWith("Exception", StringComparison.Ordinal)))
                    .ToArray();

                Assert.That(
                    customExceptions,
                    Is.Empty,
                    $"{source}: custom exception types are not allowed; use a base class library exception.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenPotentiallyFailingCastsUseTheAsOperator()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<CastExpressionSyntax> hardCasts = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<CastExpressionSyntax>()
                    .Where(cast => !IsAssignmentTarget(cast))
                    .Where(cast => IsPotentiallyFailingCast(cast))
                    .ToArray();

                Assert.That(
                    hardCasts,
                    Is.Empty,
                    $"{source}: potentially failing casts must use 'as', reserving direct casts for guaranteed conversions.");
            }
        }

        private static bool IsPredefinedCast(CastExpressionSyntax cast)
            => cast.Type is PredefinedTypeSyntax ||
                cast.Type.ToString() is "string" or "object" or "int" or "long" or "double" or "decimal" or "bool";

        private static bool IsNullLiteral(ExpressionSyntax expression)
            => expression.IsKind(SyntaxKind.NullLiteralExpression);

        private static bool IsAssignmentTarget(CastExpressionSyntax cast)
            => cast.Parent is AssignmentExpressionSyntax assignment &&
                ReferenceEquals(assignment.Left, cast);

        private static bool IsPotentiallyFailingCast(CastExpressionSyntax cast)
        {
            string targetType = cast.Type.ToString();

            return targetType is not null &&
                !targetType.StartsWith("I", StringComparison.Ordinal) &&
                !IsPredefinedCast(cast) &&
                cast.Expression is IdentifierNameSyntax;
        }

        private static bool IsBclException(string typeName)
        {
            string[] permitted =
            [
                "Exception",
                "ArgumentException",
                "ArgumentNullException",
                "ArgumentOutOfRangeException",
                "InvalidOperationException",
                "NotSupportedException",
                "NotImplementedException",
                "KeyNotFoundException",
                "IndexOutOfRangeException",
                "FormatException",
                "ObjectDisposedException",
                "TimeoutException",
                "OperationCanceledException",
                "HttpRequestException",
                "AuthenticationException",
                "FileNotFoundException",
                "DirectoryNotFoundException",
                "InvalidCastException",
                "UnauthorizedAccessException",
                "IOException"
            ];

            string simpleName = typeName.Contains('.', StringComparison.Ordinal)
                ? typeName.Substring(typeName.LastIndexOf('.') + 1)
                : typeName;

            return permitted.Contains(simpleName, StringComparer.Ordinal);
        }
    }
}