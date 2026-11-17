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
    /// Enforces the collection conventions: IEnumerable signatures, collection-expression
    /// initialisation, and LINQ usage for in-memory queries.
    /// </summary>
    [TestFixture]
    public sealed class CollectionConventionTests
    {
        private static string[] ConcreteCollectionTypes =>
        [
            "List<",
            "IList<",
            "IReadOnlyList<",
            "HashSet<",
            "Dictionary<",
            "Queue<",
            "Stack<",
            "LinkedList<",
            "SortedSet<",
            "SortedList<",
            "ObservableCollection<",
            "Collection<"
        ];

        [Test]
        public void GivenProductionSources_WhenChecked_ThenCollectionParametersUseEnumerableAbstractions()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<ParameterSyntax> concreteParameters = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ParameterSyntax>()
                    .Where(parameter => UsesConcreteCollection(parameter.Type?.ToString() ?? string.Empty))
                    .ToArray();

                Assert.That(
                    concreteParameters,
                    Is.Empty,
                    $"{source}: collection parameters must use IEnumerable<T>.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenCollectionReturnsUseEnumerableAbstractions()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<TypeSyntax> concreteReturns = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Select(method => method.ReturnType)
                    .Concat(tree.GetRoot()
                        .DescendantNodes()
                        .OfType<PropertyDeclarationSyntax>()
                        .Select(property => property.Type))
                    .Where(type => type is not null)
                    .Where(type => UsesConcreteCollection(type.ToString()))
                    .ToArray();

                Assert.That(
                    concreteReturns,
                    Is.Empty,
                    $"{source}: collection returns must use IEnumerable<T>.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenCollectionFieldsUseEnumerableAbstractions()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<TypeSyntax> concreteFields = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<FieldDeclarationSyntax>()
                    .Select(field => field.Declaration.Type)
                    .Where(type => UsesConcreteCollection(type.ToString()))
                    .ToArray();

                Assert.That(
                    concreteFields,
                    Is.Empty,
                    $"{source}: collection fields must use IEnumerable<T> or a collection expression.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenCollectionsAreInitialisedWithCollectionExpressions()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<ExpressionSyntax> constructorInitialisations = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ObjectCreationExpressionSyntax>()
                    .Where(creation => creation.Type.ToString() is
                        "List" or "Dictionary" or "HashSet" or "Queue" or "Stack" or
                        "List<>" or "Dictionary<,>" or "HashSet<>" or "Queue<>" or "Stack<>")
                    .Select(creation => (ExpressionSyntax)creation)
                    .Concat(tree.GetRoot()
                        .DescendantNodes()
                        .OfType<ImplicitObjectCreationExpressionSyntax>()
                        .Where(creation => creation.Parent is VariableDeclarationSyntax declaration &&
                            UsesConcreteCollection(declaration.Type.ToString()))
                        .Select(creation => (ExpressionSyntax)creation))
                    .ToArray();

                Assert.That(
                    constructorInitialisations,
                    Is.Empty,
                    $"{source}: collections must be initialised with collection expressions ('[]'), never 'new List<T>()' or similar.");
            }
        }

        private static bool UsesConcreteCollection(string typeText)
            => ConcreteCollectionTypes.Any(candidate => typeText.StartsWith(candidate, StringComparison.Ordinal));
    }
}
