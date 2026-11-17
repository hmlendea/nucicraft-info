using System;
using System.Collections.Generic;
using System.Linq;

using ArchUnitNET.Domain;
using ArchUnitNET.Fluent.Slices;
using ArchUnitNET.NUnit;

using NUnit.Framework;

using static ArchUnitNET.Fluent.Slices.SliceRuleDefinition;

namespace NuciCraftInfo.ArchitectureTests
{
    [TestFixture]
    public sealed class LayerDependencyTests
    {
        private static string[] DocumentedNamespaces =>
        [
            "NuciCraftInfo",
            "NuciCraftInfo.Components",
            "NuciCraftInfo.Endpoints",
            "NuciCraftInfo.Requests",
            "NuciCraftInfo.Responses",
            "NuciCraftInfo.Service"
        ];

        [Test]
        public void GivenProductionAssembly_WhenLoaded_ThenItContainsEveryDocumentedNamespace()
        {
            IReadOnlyList<string> namespaces = ProductionArchitecture.Model.Namespaces
                .Select(namespaceDefinition => namespaceDefinition.FullName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(namespaces, Is.Not.Empty, "The production architecture must declare namespaces.");

            foreach (string documentedNamespace in DocumentedNamespaces)
            {
                Assert.That(
                    namespaces,
                    Does.Contain(documentedNamespace),
                    $"The documented namespace '{documentedNamespace}' must contain production types.");
            }
        }

        [Test]
        public void GivenTheProductionAssembly_WhenItsNamespacesAreSliced_ThenNoDependencyCycleExists()
        {
            SliceRule rule = Slices()
                .Matching("NuciCraftInfo.(**)")
                .Should().BeFreeOfCycles();

            rule.Check(ProductionArchitecture.Model);
        }

        [Test]
        public void GivenTheProductionAssembly_WhenItsTopLevelNamespacesAreSliced_ThenNoDependencyCycleExists()
        {
            SliceRule rule = Slices()
                .Matching("NuciCraftInfo.(*)")
                .Should().BeFreeOfCycles();

            rule.Check(ProductionArchitecture.Model);
        }
    }
}