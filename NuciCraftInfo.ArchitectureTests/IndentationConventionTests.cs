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
    /// Enforces the indentation and line-splitting conventions: width thresholds,
    /// continuation indentation, operator placement, arrow placement, and member spacing.
    /// </summary>
    [TestFixture]
    public sealed class IndentationConventionTests
    {
        private const int ConditionWidthLimit = 72;

        private const int SignatureWidthLimit = 96;

        [Test]
        public void GivenProductionSources_WhenChecked_ThenContinuationOperatorsArePlacedAtLineEnd()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                string[] lines = SourceSyntax.ReadProductionSource(source).Split('\n');

                for (int index = 0; index < lines.Length; index++)
                {
                    string trimmed = lines[index].TrimStart();

                    Assert.That(
                        trimmed.StartsWith("&&", StringComparison.Ordinal) ||
                            trimmed.StartsWith("||", StringComparison.Ordinal) ||
                            trimmed.StartsWith("+ ", StringComparison.Ordinal) ||
                            trimmed.StartsWith("? ", StringComparison.Ordinal),
                        Is.False,
                        $"{source}:{index + 1}: continuation operators must be placed at the end of the preceding line.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenLongConditionsAreSplitOneClausePerLine()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<IfStatementSyntax> unsplitConditions = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<IfStatementSyntax>()
                    .Where(statement => !SourceSyntax.SpansMultipleLines(statement.Condition))
                    .Where(statement => SourceSyntax.Flatten(statement.Condition).Length > ConditionWidthLimit)
                    .ToArray();

                Assert.That(
                    unsplitConditions,
                    Is.Empty,
                    $"{source}: conditions exceeding {ConditionWidthLimit} characters must be split with one clause per line.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenLongParameterListsAreSplitOneParameterPerLine()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<BaseParameterListSyntax> unsplitLists = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BaseParameterListSyntax>()
                    .Where(list => list.Parameters.Count > 1)
                    .Where(list => !SourceSyntax.SpansMultipleLines(list))
                    .Where(list => SourceSyntax.Flatten(list).Length > SignatureWidthLimit)
                    .ToArray();

                Assert.That(
                    unsplitLists,
                    Is.Empty,
                    $"{source}: parameter lists exceeding {SignatureWidthLimit} characters must be split with one parameter per line.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenLongArgumentListsAreSplitOneArgumentPerLine()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<ArgumentListSyntax> unsplitLists = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ArgumentListSyntax>()
                    .Where(list => list.Arguments.Count > 1)
                    .Where(list => !SourceSyntax.SpansMultipleLines(list))
                    .Where(list => SourceSyntax.Flatten(list).Length > SignatureWidthLimit)
                    .ToArray();

                Assert.That(
                    unsplitLists,
                    Is.Empty,
                    $"{source}: argument lists exceeding {SignatureWidthLimit} characters must be split with one argument per line.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenLongExpressionBodiedSignaturesPlaceTheArrowOnTheNextLine()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<MemberDeclarationSyntax> unsplitSignatures = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => method.ExpressionBody is not null)
                    .Where(method => !SourceSyntax.SpansMultipleLines(method.ExpressionBody!))
                    .Where(method => SourceSyntax.Flatten(method).Length > SignatureWidthLimit)
                    .Select(method => (MemberDeclarationSyntax)method)
                    .Concat(tree.GetRoot()
                        .DescendantNodes()
                        .OfType<PropertyDeclarationSyntax>()
                        .Where(property => property.ExpressionBody is not null)
                        .Where(property => !SourceSyntax.SpansMultipleLines(property.ExpressionBody!))
                        .Where(property => SourceSyntax.Flatten(property).Length > SignatureWidthLimit)
                        .Select(property => (MemberDeclarationSyntax)property))
                    .ToArray();

                Assert.That(
                    unsplitSignatures,
                    Is.Empty,
                    $"{source}: signatures exceeding {SignatureWidthLimit} characters must place '=>' on the next line.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenContinuationLinesIndentByFourSpaces()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                string[] lines = SourceSyntax.ReadProductionSource(source).Split('\n');

                for (int index = 0; index < lines.Length; index++)
                {
                    string trimmed = lines[index].TrimStart();

                    if (!trimmed.StartsWith(".", StringComparison.Ordinal) &&
                        !trimmed.StartsWith("&&", StringComparison.Ordinal) &&
                        !trimmed.StartsWith("||", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    int leadingSpaces = lines[index].Length - trimmed.Length;

                    Assert.That(
                        leadingSpaces % 4,
                        Is.EqualTo(0),
                        $"{source}:{index + 1}: continuation lines must be indented in multiples of four spaces.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenPropertiesAreSeparatedFromAdjacentMembers()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                foreach (TypeDeclarationSyntax typeDeclaration in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    IReadOnlyList<MemberDeclarationSyntax> members = typeDeclaration.Members.ToArray();

                    for (int index = 0; index < members.Count; index++)
                    {
                        if (members[index] is not PropertyDeclarationSyntax)
                        {
                            continue;
                        }

                        if (index > 0)
                        {
                            Assert.That(
                                members[index].GetLocation().GetLineSpan().StartLinePosition.Line -
                                    members[index - 1].GetLocation().GetLineSpan().EndLinePosition.Line,
                                Is.GreaterThan(1),
                                $"{source}: properties must be separated from the preceding member by one blank line.");
                        }

                        if (index < members.Count - 1)
                        {
                            Assert.That(
                                members[index + 1].GetLocation().GetLineSpan().StartLinePosition.Line -
                                    members[index].GetLocation().GetLineSpan().EndLinePosition.Line,
                                Is.GreaterThan(1),
                                $"{source}: properties must be separated from the following member by one blank line.");
                        }
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenPropertyAccessorBodiesOccupyTheirOwnLines()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<AccessorDeclarationSyntax> inlineAccessors = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<AccessorDeclarationSyntax>()
                    .Where(accessor => accessor.Body is not null)
                    .Where(accessor => accessor.Body!.Statements.Count > 0)
                    .Where(accessor => accessor.Body!.Statements[0]
                        .GetLocation().GetLineSpan().StartLinePosition.Line ==
                        accessor.Keyword.GetLocation().GetLineSpan().StartLinePosition.Line)
                    .ToArray();

                Assert.That(
                    inlineAccessors,
                    Is.Empty,
                    $"{source}: each accessor body must begin on its own line.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenExactlyOneBlankLineSeparatesMethods()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                foreach (TypeDeclarationSyntax typeDeclaration in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    IReadOnlyList<MemberDeclarationSyntax> members = typeDeclaration.Members.ToArray();

                    for (int index = 0; index < members.Count - 1; index++)
                    {
                        if (members[index] is not BaseMethodDeclarationSyntax)
                        {
                            continue;
                        }

                        int gap = members[index + 1].GetLocation().GetLineSpan().StartLinePosition.Line -
                            members[index].GetLocation().GetLineSpan().EndLinePosition.Line;

                        Assert.That(
                            gap,
                            Is.EqualTo(2),
                            $"{source}: exactly one blank line must separate consecutive methods.");
                    }
                }
            }
        }
    }
}
