using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.NUnit;

using NUnit.Framework;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace NuciCraftInfo.ArchitectureTests
{
    /// <summary>
    /// Enforces the compiled boundary and usage rules that require the production assembly
    /// rather than the sources: framework isolation, HTTP creation paths, network utilities,
    /// and listener address selection.
    /// </summary>
    [TestFixture]
    public sealed class CompiledUsageConventionTests
    {
        private static string[] ForbiddenFrameworkAssemblies =>
        [
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Amazon.DynamoDBv2",
            "Dapper",
            "MediatR",
            "NUnit",
            "Moq",
            "ArchUnit",
            "xunit",
            "Microsoft.VisualStudio.TestPlatform"
        ];

        private static string[] PlatformSpecificAssemblies =>
        [
            "System.Windows.Forms",
            "System.Drawing",
            "System.Data",
            "Microsoft.Win32",
            "System.ServiceProcess"
        ];

        [TestCase("Microsoft.EntityFrameworkCore")]
        [TestCase("Amazon.DynamoDBv2")]
        [TestCase("Dapper")]
        [TestCase("MediatR")]
        [TestCase("NUnit")]
        [TestCase("Moq")]
        [TestCase("ArchUnit")]
        [TestCase("xunit")]
        [TestCase("Microsoft.VisualStudio.TestPlatform")]
        public void GivenProductionAssembly_WhenCheckingFrameworkIsolation_ThenTheAssemblyIsNotReferenced(string forbiddenAssembly)
        {
            AssemblyName[] references = ProductionArchitecture.Assembly.GetReferencedAssemblies();

            Assert.That(references, Is.Not.Empty, "The production assembly references must be inspected.");

            string[] matched = references
                .Select(reference => reference.Name ?? string.Empty)
                .Where(reference => reference.Contains(forbiddenAssembly, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.That(matched, Is.Empty, $"The production assembly must not reference {forbiddenAssembly}.");
        }

        [TestCase("System.Windows.Forms")]
        [TestCase("System.Drawing")]
        [TestCase("System.Data")]
        [TestCase("Microsoft.Win32")]
        public void GivenProductionAssembly_WhenCheckingPortability_ThenPlatformSpecificAssembliesAreNotReferenced(string forbiddenAssembly)
        {
            AssemblyName[] references = ProductionArchitecture.Assembly.GetReferencedAssemblies();

            Assert.That(references, Is.Not.Empty, "The production assembly references must be inspected.");

            string[] matched = references
                .Select(reference => reference.Name ?? string.Empty)
                .Where(reference => reference.Contains(forbiddenAssembly, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.That(matched, Is.Empty, $"The production assembly must stay platform-independent and must not reference {forbiddenAssembly}.");
        }

        [Test]
        public void GivenProductionAssembly_WhenCheckingNetworking_ThenNuciWebHttpIsReferenced()
        {
            AssemblyName[] references = ProductionArchitecture.Assembly.GetReferencedAssemblies();

            Assert.That(
                references.Select(reference => reference.Name ?? string.Empty),
                Does.Contain("NuciWeb.HTTP"),
                "The production assembly must create its HTTP clients through NuciWeb.HTTP.");
        }

        [Test]
        public void GivenProductionAssembly_WhenInspectingHttpUsage_ThenHttpClientCreatorIsTheOnlyCreationPath()
        {
            IReadOnlyList<string> sources = SourceSyntax.GetProductionSources();
            List<string> offenders = [];

            foreach (string source in sources)
            {
                string content = SourceSyntax.ReadProductionSource(source);

                bool createsHttpClient = content.Contains("new HttpClient(", StringComparison.Ordinal) ||
                    content.Contains("new HttpClientHandler(", StringComparison.Ordinal);

                if (!createsHttpClient)
                {
                    continue;
                }

                if (!content.Contains("HttpClientCreator", StringComparison.Ordinal))                {
                    offenders.Add(source);
                }
            }

            Assert.That(
                offenders,
                Is.Empty,
                $"HTTP clients must be instantiated through NuciWeb.HTTP's HttpClientCreator -> {string.Join(", ", offenders)}.");
        }

        [Test]
        public void GivenProductionSources_WhenInspectingNetworkUtilities_ThenNuciWebHttpHelpersAreUsed()
        {
            string[] forbiddenCalls = ["Dns.GetHostEntry", "Dns.GetHostAddresses", "WebRequest.Create"];

            foreach (string source in SourceSyntax.GetProductionSources())
            {
                string content = SourceSyntax.ReadProductionSource(source);

                foreach (string forbiddenCall in forbiddenCalls)
                {
                    Assert.That(
                        content,
                        Does.Not.Contain(forbiddenCall),
                        $"{source}: host and network lookups must use NuciWeb.HTTP's NetworkUtils instead of {forbiddenCall}.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenKestrelListens_ThenIPv6ListenersUseIPv6Any()
        {
            foreach (string source in SourceSyntax.GetProductionSources())
            {
                string content = SourceSyntax.ReadProductionSource(source);

                bool listensOnAnyAddress = content.Contains("IPAddress.Any", StringComparison.Ordinal) &&
                    !content.Contains("IPAddress.IPv6Any", StringComparison.Ordinal);

                Assert.That(
                    listensOnAnyAddress,
                    Is.False,
                    $"{source}: listeners expected to accept IPv6 must bind IPAddress.IPv6Any rather than IPAddress.Any.");
            }
        }

        [Test]
        public void GivenProductionAssembly_WhenCheckingPublicTypes_ThenEveryDocumentedEntryPointIsExposed()
        {
            string[] publicTypes = ProductionArchitecture.Assembly.GetTypes()
                .Where(type => type.IsPublic && !type.IsNested)
                .Where(type => type.Namespace?.StartsWith("NuciCraftInfo", StringComparison.Ordinal) == true)
                .Select(type => type.FullName ?? string.Empty)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(publicTypes, Is.Not.Empty, "The production assembly must expose public types.");

            string[] documentedEntryPoints =
            [
                "NuciCraftInfo.Endpoints.NuciCraftEndpoints",
                "NuciCraftInfo.Program",
                "NuciCraftInfo.Requests.GetServerRequest",
                "NuciCraftInfo.Responses.GetServerResponse",
                "NuciCraftInfo.Service.INuciCraftService",
                "NuciCraftInfo.Service.NuciCraftService"
            ];

            Assert.That(
                publicTypes,
                Is.SupersetOf(documentedEntryPoints),
                "Every documented entry point must remain publicly exposed.");
        }

        [Test]
        public void GivenProductionAssembly_WhenCheckingGeneratedTypes_ThenNoCompilerGeneratedPublicTypesAreExposed()
        {
            IEnumerable<string> generatedPublicTypes = ProductionArchitecture.Assembly.GetTypes()
                .Where(type => type.IsPublic && !type.IsNested)
                .Where(type => type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false))
                .Select(type => type.FullName ?? string.Empty);

            Assert.That(
                generatedPublicTypes,
                Is.Empty,
                "The production assembly must not expose compiler-generated public types.");
        }

        [Test]
        public void GivenProductionTypes_WhenCheckingTestFrameworks_ThenNoTestFrameworkTypesAreReferenced()
        {
            AssertSelectionIsNotEmpty(ProductionArchitecture.ProductionTypes);

            Types().That().Are(ProductionArchitecture.ProductionTypes)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespace("NUnit.Framework")
                        .Or().ResideInNamespace("Moq")
                        .Or().ResideInNamespace("Bunit")
                        .Or().ResideInNamespace("AngleSharp"))
                .Because("production types must not depend on test frameworks")
                .Check(ProductionArchitecture.Model);
        }

        [Test]
        public void GivenProductionTypes_WhenCheckingPresentationDependencies_ThenComponentsDoNotDependOnEndpoints()
        {
            IObjectProvider<IType> componentTypes = Types()
                .That()
                .ResideInNamespace("NuciCraftInfo.Components");

            AssertSelectionIsNotEmpty(componentTypes);

            Types().That().Are(componentTypes)
                .Should().NotDependOnAny(Types().That().ResideInNamespace("NuciCraftInfo.Endpoints"))
                .Because("presentation components must not depend on the HTTP endpoint layer")
                .Check(ProductionArchitecture.Model);
        }

        private static void AssertSelectionIsNotEmpty(IObjectProvider<IType> selection)
            => Assert.That(
                selection.GetObjects(ProductionArchitecture.Model).ToArray(),
                Is.Not.Empty,
                "The rule selector must match at least one production type.");
    }
}
