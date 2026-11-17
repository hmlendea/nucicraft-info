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
    public sealed class TypeDeclarationConventionTests
    {
        [Test]
        public void GivenProductionSources_WhenChecked_ThenEveryTypeAndMemberDeclaresExplicitAccessibility()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);
                SyntaxNode root = tree.GetRoot();

                BaseTypeDeclarationSyntax[] unannotatedTypes = root
                    .DescendantNodes()
                    .OfType<BaseTypeDeclarationSyntax>()
                    .Where(declaration => !DeclaresAccessibility(declaration.Modifiers))
                    .ToArray();

                IEnumerable<MemberDeclarationSyntax> unannotatedMembers = root
                    .DescendantNodes()
                    .OfType<MemberDeclarationSyntax>()
                    .Where(member => member is FieldDeclarationSyntax ||
                        member is PropertyDeclarationSyntax ||
                        member is EventFieldDeclarationSyntax ||
                        member is ConstructorDeclarationSyntax ||
                        member is MethodDeclarationSyntax)
                    .Where(member => !DeclaresAccessibility(GetModifiers(member)))
                    .ToArray();

                Assert.That(unannotatedTypes, Is.Empty, $"{source}: every type must declare an explicit accessibility.");
                Assert.That(unannotatedMembers, Is.Empty, $"{source}: every member must declare an explicit accessibility.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenEveryNonStaticClassIsSealed()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                ClassDeclarationSyntax[] unsealedClasses = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(declaration => !declaration.Modifiers.Any(modifier =>
                        modifier.IsKind(SyntaxKind.SealedKeyword) ||
                        modifier.IsKind(SyntaxKind.StaticKeyword) ||
                        modifier.IsKind(SyntaxKind.AbstractKeyword)))
                    .ToArray();

                Assert.That(unsealedClasses, Is.Empty, $"{source}: every non-static, non-abstract class must be sealed.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoPreviewLanguageFeaturesAreUsed()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = CSharpSyntaxTree.ParseText(
                    SourceSyntax.ReadProductionSource(source),
                    new CSharpParseOptions(LanguageVersion.Latest),
                    path: source);

                string[] errors = tree.GetDiagnostics()
                    .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                    .Where(diagnostic => diagnostic.Id is "CS8652" or "CS9202" or "CS1617")
                    .Select(diagnostic => diagnostic.GetMessage())
                    .ToArray();

                Assert.That(
                    errors,
                    Is.Empty,
                    $"{source}: preview language features are not allowed -> {string.Join("; ", errors)}");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoMarkerInterfacesOrEmptyControlBodiesAreDeclared()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);
                SyntaxNode root = tree.GetRoot();

                InterfaceDeclarationSyntax[] emptyInterfaces = root
                    .DescendantNodes()
                    .OfType<InterfaceDeclarationSyntax>()
                    .Where(declaration => declaration.Members.Count == 0)
                    .ToArray();

                IEnumerable<BlockSyntax> emptyControlBodies = root
                    .DescendantNodes()
                    .OfType<BlockSyntax>()
                    .Where(block => block.Statements.Count == 0)
                    .Where(block => block.Parent is IfStatementSyntax ||
                        block.Parent is ForStatementSyntax ||
                        block.Parent is ForEachStatementSyntax ||
                        block.Parent is WhileStatementSyntax ||
                        block.Parent is DoStatementSyntax)
                    .ToArray();

                Assert.That(emptyInterfaces, Is.Empty, $"{source}: marker interfaces without members are not allowed.");
                Assert.That(emptyControlBodies, Is.Empty, $"{source}: empty control-flow bodies are not allowed.");
            }
        }

        private static bool DeclaresAccessibility(SyntaxTokenList modifiers)
            => modifiers.Any(modifier =>
                modifier.IsKind(SyntaxKind.PublicKeyword) ||
                modifier.IsKind(SyntaxKind.InternalKeyword) ||
                modifier.IsKind(SyntaxKind.PrivateKeyword) ||
                modifier.IsKind(SyntaxKind.ProtectedKeyword));

        private static SyntaxTokenList GetModifiers(MemberDeclarationSyntax member)
            => member switch
            {
                FieldDeclarationSyntax field => field.Modifiers,
                PropertyDeclarationSyntax property => property.Modifiers,
                EventFieldDeclarationSyntax eventField => eventField.Modifiers,
                ConstructorDeclarationSyntax constructor => constructor.Modifiers,
                MethodDeclarationSyntax method => method.Modifiers,
                _ => default
            };
    }
}