using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NuciCraftInfo.ArchitectureTests
{
    internal static class SourceSyntax
    {
        private static readonly Lazy<CSharpCompilation> AnalysisCompilation = new(BuildCompilation);

        private static readonly Lazy<IReadOnlyList<SyntaxTree>> ParsedTrees = new(BuildTrees);

        internal static CSharpCompilation Compilation => AnalysisCompilation.Value;
        internal static IReadOnlyList<string> GetProductionSources()
            => GetSources(ArchitectureResources.ProductionSourcePrefix);

        internal static IReadOnlyList<string> GetSources(string prefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(prefix);

            if (sources.Count == 0)
            {
                throw new InvalidOperationException($"No embedded sources were found for the '{prefix}' prefix.");
            }

            return sources;
        }

        internal static string ReadProductionSource(string source)
            => ArchitectureResources.ReadResource(ArchitectureResources.ProductionSourcePrefix, source);

        internal static string ReadSource(string prefix, string source)
            => ArchitectureResources.ReadResource(prefix, source);

        internal static SyntaxTree ParseProductionSource(string source)
            => CSharpSyntaxTree.ParseText(ReadProductionSource(source), path: source);

        internal static SyntaxTree ParseSource(string prefix, string source)
            => CSharpSyntaxTree.ParseText(ReadSource(prefix, source), path: source);

        internal static string GetFileStem(string source)
        {
            string fileName = source.Substring(source.LastIndexOf('/') + 1);

            return fileName.Substring(0, fileName.LastIndexOf('.'));
        }

        internal static bool IsPascalCase(string name)
            => !string.IsNullOrEmpty(name) && char.IsUpper(name[0]);

        internal static bool HasModifier(SyntaxTokenList modifiers, SyntaxKind kind)
            => modifiers.Any(modifier => modifier.IsKind(kind));

        internal static bool IsPublicOrProtected(SyntaxTokenList modifiers)
            => HasModifier(modifiers, SyntaxKind.PublicKeyword) ||
                HasModifier(modifiers, SyntaxKind.ProtectedKeyword);

        internal static IReadOnlyList<string> GetTestSources()
            => AllTestPrefixes
                .SelectMany(prefix => GetSources(prefix).Select(source => prefix + source))
                .ToArray();

        internal static IReadOnlyList<string> GetAllSources()
            => GetProductionSources()
                .Select(source => ArchitectureResources.ProductionSourcePrefix + source)
                .Concat(GetTestSources())
                .ToArray();

        internal static IReadOnlyList<SyntaxTree> GetAllTrees() => ParsedTrees.Value;

        internal static IEnumerable<INamedTypeSymbol> GetDeclaredTypeSymbols()
        {
            foreach (SyntaxTree tree in GetAllTrees())
            {
                SemanticModel model = Compilation.GetSemanticModel(tree);

                foreach (TypeDeclarationSyntax declaration in tree.GetRoot()
                    .DescendantNodes()
                    .OfType<TypeDeclarationSyntax>())
                {
                    if (model.GetDeclaredSymbol(declaration) is INamedTypeSymbol symbol)
                    {
                        yield return symbol;
                    }
                }
            }
        }

        internal static bool IsProductionTree(SyntaxTree tree)
            => tree.FilePath.StartsWith(ArchitectureResources.ProductionSourcePrefix, StringComparison.Ordinal);

        internal static string ReadAnySource(string source)
        {
            foreach (string prefix in AllPrefixes)
            {
                if (source.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return ReadSource(prefix, source.Substring(prefix.Length));
                }
            }

            throw new InvalidOperationException($"The source '{source}' does not carry a known resource prefix.");
        }

        internal static SyntaxTree ParseAnySource(string source)
            => CSharpSyntaxTree.ParseText(ReadAnySource(source), path: source);

        internal static string Flatten(SyntaxNode node)
            => string.Join(" ", node.ToString().Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

        internal static bool SpansMultipleLines(SyntaxNode node)
            => node.GetLocation().GetLineSpan().StartLinePosition.Line !=
                node.GetLocation().GetLineSpan().EndLinePosition.Line;

        internal static IReadOnlyList<string> GetSourceLines(SyntaxNode node)
        {
            FileLinePositionSpan span = node.GetLocation().GetLineSpan();
            IReadOnlyList<string> lines = node.SyntaxTree.GetText().Lines.Select(line => line.ToString()).ToArray();

            return lines
                .Skip(span.StartLinePosition.Line)
                .Take(span.EndLinePosition.Line - span.StartLinePosition.Line + 1)
                .ToArray();
        }

        internal static bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
            => attributeLists
                .SelectMany(attributeList => attributeList.Attributes)
                .Any(attribute => string.Equals(attribute.Name.ToString(), attributeName, StringComparison.Ordinal));

        internal static int CountAttributes(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
            => attributeLists
                .SelectMany(attributeList => attributeList.Attributes)
                .Count(attribute => string.Equals(attribute.Name.ToString(), attributeName, StringComparison.Ordinal));

        internal static bool DeclaresAccessibility(SyntaxTokenList modifiers)
            => modifiers.Any(modifier =>
                modifier.IsKind(SyntaxKind.PublicKeyword) ||
                modifier.IsKind(SyntaxKind.InternalKeyword) ||
                modifier.IsKind(SyntaxKind.PrivateKeyword) ||
                modifier.IsKind(SyntaxKind.ProtectedKeyword));

        internal static SyntaxTokenList GetModifiers(MemberDeclarationSyntax member)
            => member switch
            {
                BaseTypeDeclarationSyntax declaration => declaration.Modifiers,
                BaseMethodDeclarationSyntax method => method.Modifiers,
                BasePropertyDeclarationSyntax property => property.Modifiers,
                BaseFieldDeclarationSyntax field => field.Modifiers,
                DelegateDeclarationSyntax delegateDeclaration => delegateDeclaration.Modifiers,
                _ => default
            };

        private static IReadOnlyList<string> AllPrefixes
            => [
                ArchitectureResources.ProductionSourcePrefix,
                ArchitectureResources.UnitTestSourcePrefix,
                ArchitectureResources.IntegrationTestSourcePrefix
            ];

        private static IReadOnlyList<string> AllTestPrefixes
            => [
                ArchitectureResources.UnitTestSourcePrefix,
                ArchitectureResources.IntegrationTestSourcePrefix
            ];

        private static IReadOnlyList<SyntaxTree> BuildTrees()
            => GetAllSources().Select(ParseAnySource).ToArray();

        private static CSharpCompilation BuildCompilation()
        {
            List<MetadataReference> references = [];

            foreach (string assemblyPath in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
            {
                try
                {
                    references.Add(MetadataReference.CreateFromFile(assemblyPath));
                }
                catch (BadImageFormatException)
                {
                    continue;
                }
            }

            return CSharpCompilation.Create(
                "NuciCraftInfo.Analysis",
                GetAllTrees(),
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}