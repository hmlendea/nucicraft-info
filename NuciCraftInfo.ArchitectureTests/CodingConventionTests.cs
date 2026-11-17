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
    public sealed class CodingConventionTests
    {
        [Test]
        public void GivenProductionSources_WhenChecked_ThenExplicitTypesAreUsed()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<VariableDeclarationSyntax> varDeclarations = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<VariableDeclarationSyntax>()
                    .Where(declaration => declaration.Type.IsVar)
                    .ToArray();

                Assert.That(varDeclarations, Is.Empty, $"{source}: the 'var' keyword must not be used.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoTernariesAreUsed()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                Assert.That(
                    tree.GetRoot().DescendantNodes().OfType<ConditionalExpressionSyntax>().ToArray(),
                    Is.Empty,
                    $"{source}: ternary and nested conditional expressions are not allowed.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoTuplesAreUsed()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<SyntaxNode> tuples = tree.GetRoot()
                    .DescendantNodes()
                    .Where(node => node.IsKind(SyntaxKind.TupleType) || node.IsKind(SyntaxKind.TupleExpression))
                    .ToArray();

                Assert.That(tuples, Is.Empty, $"{source}: tuples are not allowed.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoRegionsOrNullableDirectivesAreUsed()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                string content = SourceSyntax.ReadProductionSource(source);

                Assert.That(content, Does.Not.Match(@"(?m)^\s*#(end)?region\b"), $"{source}: regions are not allowed.");
                Assert.That(content, Does.Not.Match(@"(?m)^\s*#nullable\s+"), $"{source}: nullable directives are not allowed.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoConstDeclarationsAreUsed()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<FieldDeclarationSyntax> constFields = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<FieldDeclarationSyntax>()
                    .Where(field => SourceSyntax.HasModifier(field.Modifiers, SyntaxKind.ConstKeyword))
                    .ToArray();

                Assert.That(constFields, Is.Empty, $"{source}: 'const' is not allowed; use static properties.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoRecordTypesOrInitAccessorsAreUsed()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<RecordDeclarationSyntax> records = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<RecordDeclarationSyntax>()
                    .ToArray();

                IEnumerable<AccessorDeclarationSyntax> initAccessors = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<AccessorDeclarationSyntax>()
                    .Where(accessor => accessor.IsKind(SyntaxKind.InitAccessorDeclaration))
                    .ToArray();

                Assert.That(records, Is.Empty, $"{source}: record types are not allowed.");
                Assert.That(initAccessors, Is.Empty, $"{source}: init-only accessors are not allowed.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoGlobalOrStaticUsingsAreUsed()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<UsingDirectiveSyntax> forbiddenUsings = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<UsingDirectiveSyntax>()
                    .Where(usingDirective =>
                        usingDirective.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) ||
                        usingDirective.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword))
                    .ToArray();

                Assert.That(forbiddenUsings, Is.Empty, $"{source}: 'using static' and 'global using' directives are not allowed.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoPartialDeclarationsAreUsed()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<TypeDeclarationSyntax> partialTypes = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .Where(declaration => SourceSyntax.HasModifier(declaration.Modifiers, SyntaxKind.PartialKeyword))
                    .ToArray();

                Assert.That(partialTypes, Is.Empty, $"{source}: partial types are not allowed.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoInlineFullyQualifiedTypesAreUsed()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                string content = SourceSyntax.ReadProductionSource(source);

                Assert.That(
                    content,
                    Does.Not.Match(@"\bSystem\.(String|Int32|Int64|Boolean|Object|Byte|Char|Double|Single|Decimal|DateTime|Exception|Guid|Convert|Console|Environment|Math)\b"),
                    $"{source}: inline fully qualified types are not allowed; add a using directive instead.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoRefOrOutOrOptionalParametersAreUsed()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<ParameterSyntax> refParameters = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ParameterSyntax>()
                    .Where(parameter => parameter.Modifiers.Any(modifier =>
                        modifier.IsKind(SyntaxKind.RefKeyword) ||
                        modifier.IsKind(SyntaxKind.OutKeyword)))
                    .ToArray();

                IEnumerable<ParameterSyntax> optionalParameters = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ParameterSyntax>()
                    .Where(parameter => parameter.Default is not null)
                    .ToArray();

                Assert.That(refParameters, Is.Empty, $"{source}: ref/out parameters are not allowed.");
                Assert.That(optionalParameters, Is.Empty, $"{source}: optional parameters are not allowed; use overloads.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNoLambdasHaveMoreThanSevenParameters()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<AnonymousFunctionExpressionSyntax> oversizedLambdas = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<AnonymousFunctionExpressionSyntax>()
                    .Where(lambda => GetLambdaParameterCount(lambda) > 7)
                    .ToArray();

                Assert.That(oversizedLambdas, Is.Empty, $"{source}: lambdas must have at most seven parameters.");
            }
        }

        private static int GetLambdaParameterCount(AnonymousFunctionExpressionSyntax lambda)
        {
            ParenthesizedLambdaExpressionSyntax? parenthesizedLambda = lambda as ParenthesizedLambdaExpressionSyntax;

            if (parenthesizedLambda is null)
            {
                return 1;
            }

            return parenthesizedLambda.ParameterList.Parameters.Count;
        }
    }
}