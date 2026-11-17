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
    public sealed class SourceFileConventionTests
    {
        private static string RootNamespace => "NuciCraftInfo";

        [Test]
        public void GivenProductionSources_WhenChecked_ThenEveryFileDeclaresOneMatchingType()
        {
            IEnumerable<string> sources = SourceSyntax.GetProductionSources();

            Assert.That(sources, Is.Not.Empty, "No production source files were found.");

            foreach (string source in sources)
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);
                BaseTypeDeclarationSyntax[] declarations = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BaseTypeDeclarationSyntax>()
                    .ToArray();

                Assert.That(
                    declarations,
                    Has.Length.EqualTo(1),
                    $"{source}: expected exactly one declared type, but found {declarations.Length}.");

                Assert.That(
                    declarations[0].Identifier.ValueText,
                    Is.EqualTo(SourceSyntax.GetFileStem(source)),
                    $"{source}: the declared type must match the file name.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenTheyUseBlockNamespaces()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                Assert.That(
                    tree.GetRoot().DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().ToArray(),
                    Is.Empty,
                    $"{source}: file-scoped namespaces are not allowed.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenTheNamespaceMatchesTheFolderPath()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);
                NamespaceDeclarationSyntax[] namespaces = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<NamespaceDeclarationSyntax>()
                    .ToArray();

                Assert.That(
                    namespaces,
                    Has.Length.EqualTo(1),
                    $"{source}: the file must declare exactly one block namespace.");

                Assert.That(
                    namespaces[0].Name.ToString(),
                    Is.EqualTo(GetExpectedNamespace(source)),
                    $"{source}: the namespace must follow the folder path.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoNestedTypesOrTopLevelStatementsExist()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);
                SyntaxNode root = tree.GetRoot();

                Assert.That(
                    root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
                        .Where(declaration => declaration.Parent is TypeDeclarationSyntax)
                        .ToArray(),
                    Is.Empty,
                    $"{source}: nested types are not allowed.");

                Assert.That(
                    root.DescendantNodes().OfType<GlobalStatementSyntax>().ToArray(),
                    Is.Empty,
                    $"{source}: top-level statements are not allowed.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenControlFlowBodiesUseBraces()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<StatementSyntax> unbracedStatements = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<IfStatementSyntax>()
                    .Where(statement => statement.Statement is not BlockSyntax)
                    .Select(statement => statement.Statement)
                    .Concat(tree.GetRoot().DescendantNodes().OfType<ForStatementSyntax>()
                        .Where(statement => statement.Statement is not BlockSyntax)
                        .Select(statement => statement.Statement))
                    .Concat(tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>()
                        .Where(statement => statement.Statement is not BlockSyntax)
                        .Select(statement => statement.Statement))
                    .Concat(tree.GetRoot().DescendantNodes().OfType<WhileStatementSyntax>()
                        .Where(statement => statement.Statement is not BlockSyntax)
                        .Select(statement => statement.Statement))
                    .Concat(tree.GetRoot().DescendantNodes().OfType<DoStatementSyntax>()
                        .Where(statement => statement.Statement is not BlockSyntax)
                        .Select(statement => statement.Statement))
                    .ToArray();

                Assert.That(
                    unbracedStatements,
                    Is.Empty,
                    $"{source}: every control-flow body must use braces.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenEveryMethodHasAtMostSevenParameters()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<BaseMethodDeclarationSyntax> oversizedMethods = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BaseMethodDeclarationSyntax>()
                    .Where(method => method.ParameterList.Parameters.Count > 7)
                    .ToArray();

                IEnumerable<ConstructorDeclarationSyntax> oversizedConstructors = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ConstructorDeclarationSyntax>()
                    .Where(constructor => constructor.ParameterList.Parameters.Count > 7)
                    .ToArray();

                Assert.That(oversizedMethods, Is.Empty, $"{source}: methods must have at most seven parameters.");
                Assert.That(oversizedConstructors, Is.Empty, $"{source}: constructors must have at most seven parameters.");
            }
        }

        private static string GetExpectedNamespace(string source)
        {
            const string productionSourcePrefix = "ProductionSources/";

            string relativePath = source.StartsWith(productionSourcePrefix, StringComparison.Ordinal)
                ? source.Substring(productionSourcePrefix.Length)
                : source;

            int lastSeparatorIndex = relativePath.LastIndexOf('/');

            string folderPath = lastSeparatorIndex < 0
                ? string.Empty
                : relativePath.Substring(0, lastSeparatorIndex).Replace('/', '.');

            return string.IsNullOrEmpty(folderPath)
                ? RootNamespace
                : $"{RootNamespace}.{folderPath}";
        }
    }
}