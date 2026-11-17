using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.NUnit;

using NUnit.Framework;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace NuciCraftInfo.ArchitectureTests
{
    [TestFixture]
    public sealed class DependencyBoundaryTests
    {
        [Test]
        public void GivenProductionAssembly_WhenLoaded_ThenOnlyTheProductionAssemblyIsLoaded()
            => Assert.That(
                ProductionArchitecture.Model.Assemblies.Select(assembly => assembly.Name),
                Is.EquivalentTo(new[] { ProductionArchitecture.Assembly.GetName().Name }));

        [TestCase("Microsoft.EntityFrameworkCore")]
        [TestCase("Microsoft.EntityFrameworkCore.Design")]
        [TestCase("Amazon.DynamoDBv2")]
        [TestCase("Dapper")]
        [TestCase("MediatR")]
        [TestCase("NUnit")]
        [TestCase("Moq")]
        [TestCase("ArchUnit")]
        [TestCase("xunit")]
        public void GivenProductionAssembly_WhenCheckingExternalDependencies_ThenForbiddenAssembliesAreNotReferenced(
            string forbiddenAssembly)
        {
            AssemblyName[] references = ProductionArchitecture.Assembly.GetReferencedAssemblies();

            Assert.That(references, Is.Not.Empty, "The production assembly references must be inspected.");
            Assert.That(
                references
                    .Select(reference => reference.Name ?? string.Empty)
                    .Where(reference => reference.Contains(forbiddenAssembly, StringComparison.OrdinalIgnoreCase)),
                Is.Empty,
                $"The production assembly must not reference {forbiddenAssembly}.");
        }

        [Test]
        public void GivenProductionAssembly_WhenCheckingNetworkDependencies_ThenNuciWebHttpIsReferenced()
        {
            AssemblyName[] references = ProductionArchitecture.Assembly.GetReferencedAssemblies();

            Assert.That(references, Is.Not.Empty, "The production assembly references must be inspected.");
            Assert.That(
                references.Select(reference => reference.Name ?? string.Empty),
                Does.Contain("NuciWeb.HTTP"),
                "The production assembly must create its HTTP clients through NuciWeb.HTTP.");
        }

        [Test]
        public void GivenProductionAssembly_WhenCheckingTestDependencies_ThenTestAssembliesAreAbsent()
        {
            AssemblyName[] references = ProductionArchitecture.Assembly.GetReferencedAssemblies();

            Assert.That(references, Is.Not.Empty, "The production assembly references must be inspected.");
            Assert.That(
                references
                    .Select(reference => reference.Name ?? string.Empty)
                    .Where(IsTestDependency),
                Is.Empty,
                "The production assembly must remain independent of test assemblies and frameworks.");
        }

        [Test]
        public void GivenProductionAssembly_WhenCheckingFriendAssemblies_ThenInternalsAreNotExposed()
            => Assert.That(
                ProductionArchitecture.Assembly.GetCustomAttributesData()
                    .Where(attribute => attribute.AttributeType == typeof(InternalsVisibleToAttribute)),
                Is.Empty,
                "The production assembly must not expose internals to friend assemblies.");

        [Test]
        public void GivenProductionTypes_WhenCheckingTestDependencies_ThenNoTestNamespaceIsReferenced()
        {
            AssertSelectionIsNotEmpty(ProductionArchitecture.ProductionTypes, "the production boundary");

            Types().That().Are(ProductionArchitecture.ProductionTypes)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespace("NuciCraftInfo.UnitTests")
                        .Or().ResideInNamespace("NuciCraftInfo.IntegrationTests")
                        .Or().ResideInNamespace("NuciCraftInfo.ArchitectureTests"))
                .Because("production types must not depend on test types")
                .Check(ProductionArchitecture.Model);
        }

        [Test]
        public void GivenServiceTypes_WhenCheckingDependencies_ThenTheyDoNotDependOnEndpointsOrComponents()
        {
            IObjectProvider<IType> serviceTypes = Types()
                .That()
                .ResideInNamespace("NuciCraftInfo.Service");

            AssertSelectionIsNotEmpty(serviceTypes, "the service boundary");

            Types().That().Are(serviceTypes)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespace("NuciCraftInfo.Endpoints")
                        .Or().ResideInNamespace("NuciCraftInfo.Components"))
                .Because("the service layer must stay independent of the HTTP and presentation layers")
                .Check(ProductionArchitecture.Model);
        }

        [Test]
        public void GivenContractTypes_WhenCheckingDependencies_ThenTheyDoNotDependOnHigherLayers()
        {
            IObjectProvider<IType> contractTypes = Types()
                .That()
                .ResideInNamespace("NuciCraftInfo.Requests")
                .Or().ResideInNamespace("NuciCraftInfo.Responses");

            AssertSelectionIsNotEmpty(contractTypes, "the request and response contracts");

            Types().That().Are(contractTypes)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespace("NuciCraftInfo.Endpoints")
                        .Or().ResideInNamespace("NuciCraftInfo.Service")
                        .Or().ResideInNamespace("NuciCraftInfo.Components"))
                .Because("the API contracts must remain leaf types")
                .Check(ProductionArchitecture.Model);
        }

        [Test]
        public void GivenEndpointTypes_WhenCheckingDependencies_ThenTheyDependOnTheServiceContractOnly()
        {
            IObjectProvider<IType> endpointTypes = Types()
                .That()
                .ResideInNamespace("NuciCraftInfo.Endpoints");

            AssertSelectionIsNotEmpty(endpointTypes, "the endpoints layer");

            Types().That().Are(endpointTypes)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespace("NuciCraftInfo.Components")
                        .Or().ResideInNamespace("NuciCraftInfo.Requests"))
                .Because("the endpoints must only translate between HTTP and the service contract")
                .Check(ProductionArchitecture.Model);
        }

        [Test]
        public void GivenProductionTypes_WhenCheckingPublicSurface_ThenOnlyTheDocumentedNamespacesAreUsed()
        {
            IObjectProvider<IType> declaredTypes = Types()
                .That()
                .Are(ProductionArchitecture.ProductionTypes)
                .And()
                .ResideInNamespaceMatching(@"^NuciCraftInfo(\.|$)");

            AssertSelectionIsNotEmpty(declaredTypes, "the production boundary");

            Types().That().Are(declaredTypes)
                .Should().ResideInNamespaceMatching("^NuciCraftInfo$|^NuciCraftInfo\\.(Components|Endpoints|Requests|Responses|Service)(\\.|$)")
                .Because("production types must stay inside the documented namespaces")
                .Check(ProductionArchitecture.Model);
        }

        private static void AssertSelectionIsNotEmpty(IObjectProvider<IType> selection, string boundaryName)
        {
            IReadOnlyList<IType> selectedTypes = selection.GetObjects(ProductionArchitecture.Model).ToArray();

            Assert.That(
                selectedTypes,
                Is.Not.Empty,
                $"The {boundaryName} must select production types.");
        }

        private static bool IsTestDependency(string reference)
            => reference.Contains("Tests", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("NUnit", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("Moq", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("ArchUnit", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("Test.Sdk", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("testhost", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("coverlet", StringComparison.OrdinalIgnoreCase);
    }
}