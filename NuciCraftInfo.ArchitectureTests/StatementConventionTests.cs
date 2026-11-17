using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace NuciCraftInfo.ArchitectureTests
{
    [TestFixture]
    public sealed class StatementConventionTests
    {
        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoStatementsFollowTerminalStatements()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                foreach (BlockSyntax block in tree.GetRoot().DescendantNodes().OfType<BlockSyntax>())
                {
                    for (int index = 0; index < block.Statements.Count - 1; index++)
                    {
                        Assert.That(
                            IsTerminal(block.Statements[index]),
                            Is.False,
                            $"{source}: unreachable statements must not follow an unconditional return, throw, break, or continue.");
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenCatchBlocksAreNotEmpty()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                CatchClauseSyntax[] emptyCatches = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<CatchClauseSyntax>()
                    .Where(catchClause => catchClause.Block.Statements.Count == 0)
                    .ToArray();

                Assert.That(emptyCatches, Is.Empty, $"{source}: empty catch blocks are not allowed.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenRethrowUsesABareThrow()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                ThrowStatementSyntax[] rethrows = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<CatchClauseSyntax>()
                    .SelectMany(catchClause => catchClause.Block.DescendantNodes().OfType<ThrowStatementSyntax>())
                    .Where(throwStatement => throwStatement.Expression is not null)
                    .ToArray();

                Assert.That(rethrows, Is.Empty, $"{source}: a rethrow must use a bare 'throw;' to preserve the stack trace.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenSwitchStatementsDeclareExplicitSections()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                SwitchStatementSyntax[] emptySwitches = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<SwitchStatementSyntax>()
                    .Where(switchStatement => switchStatement.Sections.Count == 0)
                    .ToArray();

                Assert.That(emptySwitches, Is.Empty, $"{source}: switch statements must declare at least one section.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenBlankLinesSurroundBlockControlFlowStatements()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                string[] lines = SourceSyntax.ReadProductionSource(source).Split('\n');

                for (int index = 0; index < lines.Length; index++)
                {
                    if (!IsControlFlowLine(lines[index]) || index == 0 || index == lines.Length - 1)
                    {
                        continue;
                    }

                    string previous = lines[index - 1];
                    string next = lines[index + 1];

                    if (IsStructuralLine(previous) || previous.Trim().Length == 0 ||
                        IsStructuralLine(next) || IsControlFlowLine(next) || next.Trim().Length == 0)
                    {
                        continue;
                    }

                    Assert.That(
                        previous.Trim().Length == 0 && next.Trim().Length == 0,
                        Is.True,
                        $"{source}:{index + 1}: block control-flow statements must be surrounded by blank lines.");
                }
            }
        }

        private static bool IsTerminal(StatementSyntax statement)
            => statement is ReturnStatementSyntax ||
                statement is ThrowStatementSyntax ||
                statement is BreakStatementSyntax ||
                statement is ContinueStatementSyntax;

        private static bool IsControlFlowLine(string line)
        {
            string trimmed = line.TrimStart();

            return trimmed.StartsWith("if (", System.StringComparison.Ordinal) ||
                trimmed.StartsWith("foreach (", System.StringComparison.Ordinal) ||
                trimmed.StartsWith("while (", System.StringComparison.Ordinal) ||
                trimmed.StartsWith("do", System.StringComparison.Ordinal) ||
                trimmed.StartsWith("switch (", System.StringComparison.Ordinal) ||
                trimmed == "continue;" ||
                trimmed == "break;";
        }

        private static bool IsStructuralLine(string line)
        {
            string trimmed = line.Trim();

            return trimmed == "{" || trimmed == "}" || trimmed.StartsWith("else", System.StringComparison.Ordinal);
        }
    }
}