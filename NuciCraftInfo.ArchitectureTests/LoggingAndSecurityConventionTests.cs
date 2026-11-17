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
    /// Enforces the logging and security conventions: NuciLog operations, structured keys,
    /// sensitive-key masking, and the placement of logging types.
    /// </summary>
    [TestFixture]
    public sealed class LoggingAndSecurityConventionTests
    {
        private static string[] SensitiveValueIdentifiers =>
        [
            "password",
            "passphrase",
            "secret",
            "token",
            "apiKey",
            "api_key",
            "credential",
            "privateKey",
            "connectionString",
            "authorisation",
            "authorization"
        ];

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNuciLogCallsDeclareAnOperation()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<InvocationExpressionSyntax> operationlessCalls = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                        memberAccess.Name.Identifier.ValueText.StartsWith("Log", StringComparison.Ordinal))
                    .Where(invocation => invocation.ArgumentList.Arguments.Count > 0)
                    .Where(invocation => invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax)
                    .ToArray();

                Assert.That(
                    operationlessCalls,
                    Is.Empty,
                    $"{source}: every NuciLog call must receive an Operation, never an inline operation string.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenNuciLogCallsPassExceptionsThroughTheDedicatedParameter()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<InvocationExpressionSyntax> textualExceptions = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                        string.Equals(memberAccess.Name.Identifier.ValueText, "LogError", StringComparison.Ordinal))
                    .Where(invocation => invocation.ArgumentList.Arguments.Any(argument =>
                        argument.ToString().Contains(".Message", StringComparison.Ordinal) ||
                        argument.ToString().Contains(".StackTrace", StringComparison.Ordinal)))
                    .ToArray();

                Assert.That(
                    textualExceptions,
                    Is.Empty,
                    $"{source}: exceptions must be passed through the dedicated exception parameter, never embedded as text.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenCustomLoggingTypesResideInTheLoggingNamespace()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<BaseTypeDeclarationSyntax> misplacedTypes = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BaseTypeDeclarationSyntax>()
                    .Where(declaration => IsLoggingType(declaration))
                    .Where(declaration => !ResidesInLoggingNamespace(tree))
                    .ToArray();

                Assert.That(
                    misplacedTypes,
                    Is.Empty,
                    $"{source}: custom Operation and LogInfoKey types must reside in the [Project].Logging namespace.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenSensitiveKeysAreRegisteredAsSensitive()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<ClassDeclarationSyntax> insecureKeys = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(declaration => LooksLikeSensitiveLogKey(declaration))
                    .Where(declaration => !PassesSensitiveFlagToBase(declaration))
                    .ToArray();

                Assert.That(
                    insecureKeys,
                    Is.Empty,
                    $"{source}: log keys carrying secrets must pass 'true' to the protected base constructor.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenSensitiveIdentifiersAreNotEmbeddedInLogText()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                SyntaxTree tree = SourceSyntax.ParseProductionSource(source);

                IEnumerable<InterpolatedStringExpressionSyntax> sensitiveInterpolations = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<InterpolatedStringExpressionSyntax>()
                    .Where(interpolation => interpolation.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Any(identifier => SensitiveValueIdentifiers.Contains(identifier.Identifier.ValueText, StringComparer.OrdinalIgnoreCase)))
                    .Where(interpolation => IsLogOrExceptionText(interpolation))
                    .ToArray();

                Assert.That(
                    sensitiveInterpolations,
                    Is.Empty,
                    $"{source}: raw sensitive values must be masked before being embedded in log or exception text.");
            }
        }

        private static bool IsLoggingType(BaseTypeDeclarationSyntax declaration)
        {
            string name = declaration.Identifier.ValueText;

            if (!name.EndsWith("Operation", StringComparison.Ordinal) &&
                !name.EndsWith("LogInfoKey", StringComparison.Ordinal))
            {
                return false;
            }

            if (declaration.BaseList is null)
            {
                return false;
            }

            return declaration.BaseList.Types.Any(baseType =>
                baseType.Type.ToString().Contains("Operation", StringComparison.Ordinal) ||
                baseType.Type.ToString().Contains("LogInfoKey", StringComparison.Ordinal));
        }

        private static bool ResidesInLoggingNamespace(SyntaxTree tree)
            => tree.GetRoot()
                .DescendantNodes()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .Any(declaration => declaration.Name.ToString().EndsWith(".Logging", StringComparison.Ordinal));

        private static bool LooksLikeSensitiveLogKey(ClassDeclarationSyntax declaration)
        {
            if (declaration.BaseList is null)
            {
                return false;
            }

            if (!declaration.BaseList.Types.Any(baseType =>
                baseType.Type.ToString().Contains("LogInfoKey", StringComparison.Ordinal)))
            {
                return false;
            }

            return SensitiveValueIdentifiers.Any(identifier =>
                declaration.ToString().Contains(identifier, StringComparison.OrdinalIgnoreCase));
        }

        private static bool PassesSensitiveFlagToBase(ClassDeclarationSyntax declaration)
            => declaration.Members
                .OfType<ConstructorDeclarationSyntax>()
                .Any(constructor => constructor.Initializer is not null &&
                    constructor.Initializer.ToString().Contains("true", StringComparison.Ordinal));

        private static bool IsLogOrExceptionText(InterpolatedStringExpressionSyntax interpolation)
        {
            SyntaxNode? ancestor = interpolation.Parent;

            while (ancestor is not null and not MethodDeclarationSyntax)
            {
                if (ancestor is InvocationExpressionSyntax invocation &&
                    invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    string name = memberAccess.Name.Identifier.ValueText;

                    return name.StartsWith("Log", StringComparison.Ordinal) ||
                        name.EndsWith("Exception", StringComparison.Ordinal);
                }

                ancestor = ancestor.Parent;
            }

            return false;
        }
    }
}