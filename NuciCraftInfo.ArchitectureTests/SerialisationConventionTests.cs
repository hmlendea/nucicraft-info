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
    /// Enforces the serialisation conventions: culture-aware date-time operations,
    /// JsonPropertyName mappings for Identifier segments, and cached JsonSerializerOptions.
    /// </summary>
    [TestFixture]
    public sealed class SerialisationConventionTests
    {
        private static string[] DateTimeMethods =>
        [
            "Parse",
            "ParseExact",
            "TryParse",
            "TryParseExact"
        ];

        [Test]
        public void GivenProductionSources_WhenChecked_ThenDateTimeParsingReceivesAFormatProvider()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<InvocationExpressionSyntax> cultureBlindCalls = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                        memberAccess.Expression.ToString() is "DateTime" or "DateTimeOffset")
                    .Where(invocation => DateTimeMethods.Contains(
                        invocation.Expression is MemberAccessExpressionSyntax access ? access.Name.Identifier.ValueText : string.Empty,
                        StringComparer.Ordinal))
                    .Where(invocation => !invocation.ArgumentList.Arguments.Any(argument =>
                        argument.ToString().Contains("CultureInfo", StringComparison.Ordinal)))
                    .ToArray();

                Assert.That(
                    cultureBlindCalls,
                    Is.Empty,
                    $"{source}: date-time parsing must receive a format provider, such as CultureInfo.InvariantCulture.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenDateTimeFormattingReceivesAFormatProvider()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<InvocationExpressionSyntax> cultureBlindCalls = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                        memberAccess.Name.Identifier.ValueText == "ToString")
                    .Where(invocation => invocation.ArgumentList.Arguments.Count > 0)
                    .Where(invocation => invocation.ArgumentList.Arguments.Any(argument =>
                        argument.ToString().StartsWith("\"", StringComparison.Ordinal)))
                    .Where(invocation => !invocation.ArgumentList.Arguments.Any(argument =>
                        argument.ToString().Contains("CultureInfo", StringComparison.Ordinal)))
                    .ToArray();

                Assert.That(
                    cultureBlindCalls,
                    Is.Empty,
                    $"{source}: formatting with a format string must pass CultureInfo.InvariantCulture or an explicit culture.");
            }
        }

        [Test]
        public void GivenResponseContracts_WhenChecked_ThenIdentifierSegmentsSerialiseAsId()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                if (!tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>()
                    .Any(attribute => attribute.Name.ToString().EndsWith("JsonPropertyName", StringComparison.Ordinal)))
                {
                    continue;
                }

                IEnumerable<PropertyDeclarationSyntax> unmappedIdentifiers = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .Where(property => property.Identifier.ValueText.Contains("Identifier", StringComparison.Ordinal))
                    .Where(property => !HasIdJsonName(property))
                    .ToArray();

                Assert.That(
                    unmappedIdentifiers,
                    Is.Empty,
                    $"{source}: every 'Identifier' property segment must serialise as 'id' through JsonPropertyName.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenJsonSerializerOptionsAreCachedInStaticReadonlyFields()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<ObjectCreationExpressionSyntax> volatileOptions = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ObjectCreationExpressionSyntax>()
                    .Where(creation => creation.Type.ToString().Contains("JsonSerializerOptions", StringComparison.Ordinal))
                    .Where(creation => !IsInsideStaticReadonlyField(creation))
                    .ToArray();

                Assert.That(
                    volatileOptions,
                    Is.Empty,
                    $"{source}: JsonSerializerOptions must be cached in a static readonly field, never allocated per call.");
            }
        }

        private static bool HasIdJsonName(PropertyDeclarationSyntax property)
            => property.AttributeLists
                .SelectMany(attributeList => attributeList.Attributes)
                .Where(attribute => attribute.Name.ToString().EndsWith("JsonPropertyName", StringComparison.Ordinal))
                .SelectMany(attribute => attribute.ArgumentList?.Arguments ?? default)
                .Select(argument => argument.Expression)
                .OfType<LiteralExpressionSyntax>()
                .Any(literal => literal.Token.ValueText.Contains("id", StringComparison.Ordinal));

        private static bool IsInsideStaticReadonlyField(ObjectCreationExpressionSyntax creation)
        {
            SyntaxNode? ancestor = creation.Parent;

            while (ancestor is not null and not CompilationUnitSyntax)
            {
                if (ancestor is FieldDeclarationSyntax field &&
                    SourceSyntax.HasModifier(field.Modifiers, SyntaxKind.StaticKeyword) &&
                    SourceSyntax.HasModifier(field.Modifiers, SyntaxKind.ReadOnlyKeyword))
                {
                    return true;
                }

                if (ancestor is MemberDeclarationSyntax or AccessorDeclarationSyntax or ConstructorDeclarationSyntax)
                {
                    return false;
                }

                ancestor = ancestor.Parent;
            }

            return false;
        }
    }
}
