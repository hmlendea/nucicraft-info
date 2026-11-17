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
    public sealed class FormattingConventionTests
    {
        private static IEnumerable<string> GetProductionSources()
        {
            IReadOnlyList<string> sources = SourceSyntax.GetProductionSources();

            Assert.That(sources, Is.Not.Empty, "No production source files were found.");

            return sources;
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoTwoConsecutiveBlankLinesExist()
        {
            foreach (string source in GetProductionSources())
            {
                Assert.That(
                    SourceSyntax.ReadProductionSource(source),
                    Does.Not.Match(@"\n[ \t]*\n[ \t]*\n"),
                    $"{source}: two or more consecutive blank lines are not allowed.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenEachFileEndsWithExactlyOneLineFeed()
        {
            foreach (string source in GetProductionSources())
            {
                string content = SourceSyntax.ReadProductionSource(source);

                Assert.That(content.EndsWith('\n'), Is.True, $"{source}: the file must end with a line feed.");
                Assert.That(content.EndsWith("\n\n", StringComparison.Ordinal), Is.False, $"{source}: the file must end with exactly one line feed.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoCarriageReturnsOrByteOrderMarksArePresent()
        {
            foreach (string source in GetProductionSources())
            {
                string content = SourceSyntax.ReadProductionSource(source);

                Assert.That(content, Does.Not.Contain("\r"), $"{source}: line endings must use a single LF, not CRLF.");
                Assert.That(content.StartsWith('\uFEFF'), Is.False, $"{source}: the file must not start with a byte-order mark.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoTrailingWhitespaceOrTabsAreUsed()
        {
            foreach (string source in GetProductionSources())
            {
                string content = SourceSyntax.ReadProductionSource(source);

                Assert.That(content, Does.Not.Match(@"[ \t]+(\r?\n)"), $"{source}: trailing whitespace is not allowed.");
                Assert.That(content, Does.Not.Match(@"(?m)^[ \t]*\t"), $"{source}: tabs must not be used for indentation.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenIndentationUsesFourSpaceMultiples()
        {
            foreach (string source in GetProductionSources())
            {
                string[] lines = SourceSyntax.ReadProductionSource(source).Split('\n');

                for (int index = 0; index < lines.Length; index++)
                {
                    if (lines[index].Trim().Length == 0)
                    {
                        continue;
                    }

                    int leadingSpaces = lines[index].Length - lines[index].TrimStart(' ').Length;

                    Assert.That(
                        leadingSpaces % 4,
                        Is.EqualTo(0),
                        $"{source}:{index + 1}: indentation must use multiples of four spaces.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoBlankLinePrecedesClosingDelimiters()
        {
            foreach (string source in GetProductionSources())
            {
                Assert.That(
                    SourceSyntax.ReadProductionSource(source),
                    Does.Not.Match(@"\n[ \t]*\n[ \t]*[}\])]"),
                    $"{source}: no blank line may precede a closing delimiter.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenExactlyOneBlankLinePrecedesTheNamespace()
        {
            foreach (string source in GetProductionSources())
            {
                Assert.That(
                    SourceSyntax.ReadProductionSource(source),
                    Does.Not.Match(@"\n[ \t]*\n[ \t]*\n[ \t]*namespace "),
                    $"{source}: exactly one blank line must precede the namespace declaration.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenUsingDirectivesAreAlphabetisedWithinGroups()
        {
            foreach (string source in GetProductionSources())
            {
                foreach (IReadOnlyList<string> group in GetUsingGroups(source))
                {
                    string[] sorted = group.OrderBy(name => name, StringComparer.Ordinal).ToArray();

                    Assert.That(
                        group,
                        Is.EqualTo(sorted),
                        $"{source}: using directives must be alphabetised within each group.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenCommentLinesFollowTheDocumentedFormat()
        {
            foreach (string source in GetProductionSources())
            {
                string[] lines = SourceSyntax.ReadProductionSource(source).Split('\n');

                for (int index = 0; index < lines.Length; index++)
                {
                    string trimmed = lines[index].TrimStart();

                    if (!trimmed.StartsWith("//", StringComparison.Ordinal) ||
                        trimmed.StartsWith("///", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string text = trimmed.Substring(2);

                    Assert.That(text.StartsWith(' ') || text.Length == 0, Is.True, $"{source}:{index + 1}: a comment must have a space after '//'.");

                    if (text.Trim().Length == 0)
                    {
                        continue;
                    }

                    Assert.That(char.IsUpper(text.TrimStart()[0]), Is.True, $"{source}:{index + 1}: a comment must begin with an uppercase letter.");
                    Assert.That(text.TrimEnd().EndsWith('.'), Is.True, $"{source}:{index + 1}: a comment must end with a period.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenMemberAttributesArePlacedOnTheirOwnLines()
        {
            foreach (string source in GetProductionSources())
            {
                string[] lines = SourceSyntax.ReadProductionSource(source).Split('\n');
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                AttributeListSyntax[] inlineAttributes = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<AttributeListSyntax>()
                    .Where(attributeList => attributeList.Parent is MemberDeclarationSyntax)
                    .Where(attributeList => IsInlineMemberAttribute(lines, attributeList))
                    .ToArray();

                Assert.That(inlineAttributes, Is.Empty, $"{source}: member attributes must be placed on their own lines.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenElseClausesDoNotFollowTerminalStatements()
        {
            foreach (string source in GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                foreach (IfStatementSyntax ifStatement in tree.GetRoot().DescendantNodes().OfType<IfStatementSyntax>())
                {
                    if (ifStatement.Else is null)
                    {
                        continue;
                    }

                    StatementSyntax[] statements = ifStatement.Statement is BlockSyntax block
                        ? block.Statements.ToArray()
                        : [ifStatement.Statement];

                    if (statements.Length == 0)
                    {
                        continue;
                    }

                    StatementSyntax lastStatement = statements[statements.Length - 1];

                    Assert.That(
                        lastStatement is ReturnStatementSyntax || lastStatement is ThrowStatementSyntax,
                        Is.False,
                        $"{source}: an 'else' clause must not follow a terminal 'return' or 'throw'; use an early exit.");
                }
            }
        }

        private static bool IsInlineMemberAttribute(string[] lines, AttributeListSyntax attributeList)
        {
            FileLinePositionSpan span = attributeList.GetLocation().GetLineSpan();

            int lineIndex = span.EndLinePosition.Line;

            if (lineIndex >= lines.Length)
            {
                return false;
            }

            string line = lines[lineIndex];
            int endCharacter = Math.Min(span.EndLinePosition.Character, line.Length);
            string remainder = line.Substring(endCharacter).Trim();

            return remainder.Length > 0 && !remainder.StartsWith("//", StringComparison.Ordinal);
        }

        private static IReadOnlyList<IReadOnlyList<string>> GetUsingGroups(string source)
        {
            string[] lines = SourceSyntax.ReadProductionSource(source).Split('\n');

            List<IReadOnlyList<string>> groups = [];
            List<string> currentGroup = [];

            foreach (string line in lines)
            {
                bool isUsingDirective = line.StartsWith("using ", StringComparison.Ordinal) && line.EndsWith(';');

                if (isUsingDirective)
                {
                    currentGroup.Add(line.Replace("using ", string.Empty, StringComparison.Ordinal).TrimEnd(';'));
                    continue;
                }

                if (currentGroup.Count > 0)
                {
                    groups.Add(currentGroup);
                    currentGroup = [];
                }
            }

            if (currentGroup.Count > 0)
            {
                groups.Add(currentGroup);
            }

            return groups;
        }
    }
}