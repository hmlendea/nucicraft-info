using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace NuciCraftInfo.ArchitectureTests
{
    [TestFixture]
    public sealed class ExpressionBodyConventionTests
    {
        [Test]
        public void GivenProductionSources_WhenChecked_ThenSingleStatementMethodsAreExpressionBodied()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<MethodDeclarationSyntax> violations = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => method.Body is not null && method.Body.Statements.Count == 1)
                    .ToArray();

                Assert.That(
                    violations,
                    Is.Empty,
                    $"{source}: every single-statement method must be expression-bodied.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenSingleStatementGettersAreExpressionBodied()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<PropertyDeclarationSyntax> violations = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .Where(property => property.ExpressionBody is null && property.AccessorList is not null)
                    .Where(HasSingleStatementGetter)
                    .ToArray();

                Assert.That(
                    violations,
                    Is.Empty,
                    $"{source}: computed single-accessor properties must be expression-bodied.");
            }
        }

        private static bool HasSingleStatementGetter(PropertyDeclarationSyntax property)
        {
            if (property.AccessorList is null || property.AccessorList.Accessors.Count != 1)
            {
                return false;
            }

            AccessorDeclarationSyntax accessor = property.AccessorList.Accessors[0];

            return accessor.IsKind(SyntaxKind.GetAccessorDeclaration) &&
                accessor.Body is not null &&
                accessor.Body.Statements.Count == 1;
        }
    }
}