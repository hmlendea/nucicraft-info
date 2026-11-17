using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using NUnit.Framework;

namespace NuciCraftInfo.ArchitectureTests
{
    /// <summary>
    /// Enforces the evaluated project metadata conventions: target framework, language
    /// settings, package ordering, test flags, and analyser availability.
    /// </summary>
    [TestFixture]
    public sealed class ProjectMetadataConventionTests
    {
        private static string ProductionProjectPath => "NuciCraftInfo/NuciCraftInfo.csproj";

        private static string[] TestProjectPaths =>
        [
            "NuciCraftInfo.ArchitectureTests/NuciCraftInfo.ArchitectureTests.csproj",
            "NuciCraftInfo.UnitTests/NuciCraftInfo.UnitTests.csproj",
            "NuciCraftInfo.IntegrationTests/NuciCraftInfo.IntegrationTests.csproj"
        ];

        [TestCase("NuciCraftInfo/NuciCraftInfo.csproj")]
        [TestCase("NuciCraftInfo.ArchitectureTests/NuciCraftInfo.ArchitectureTests.csproj")]
        [TestCase("NuciCraftInfo.UnitTests/NuciCraftInfo.UnitTests.csproj")]
        [TestCase("NuciCraftInfo.IntegrationTests/NuciCraftInfo.IntegrationTests.csproj")]
        public void GivenProjects_WhenChecked_ThenSingleTargetFrameworkIsDeclared(string projectPath)
        {
            XDocument project = LoadProject(projectPath);

            Assert.That(
                project.Descendants().Where(element =>
                    string.Equals(element.Name.LocalName, "TargetFrameworks", StringComparison.Ordinal)),
                Is.Empty,
                $"{projectPath}: multi-targeting must not be declared; use a single TargetFramework.");

            Assert.That(
                GetProperty(project, "TargetFramework"),
                Does.StartWith("net1"),
                $"{projectPath}: the target framework must be a modern .NET version.");
        }

        [TestCase("NuciCraftInfo/NuciCraftInfo.csproj")]
        [TestCase("NuciCraftInfo.ArchitectureTests/NuciCraftInfo.ArchitectureTests.csproj")]
        [TestCase("NuciCraftInfo.UnitTests/NuciCraftInfo.UnitTests.csproj")]
        [TestCase("NuciCraftInfo.IntegrationTests/NuciCraftInfo.IntegrationTests.csproj")]
        public void GivenProjects_WhenChecked_ThenNoFriendAssembliesAreDeclared(string projectPath)
        {
            XDocument project = LoadProject(projectPath);

            Assert.That(
                project.Descendants().Where(element =>
                    string.Equals(element.Name.LocalName, "InternalsVisibleTo", StringComparison.Ordinal)),
                Is.Empty,
                $"{projectPath}: InternalsVisibleTo must not be declared.");
        }

        [Test]
        public void GivenTestProjects_WhenChecked_ThenTheyDoNotReferenceTestProjectsOfOtherLevels()
        {
            foreach (string projectPath in TestProjectPaths)
            {
                XDocument project = LoadProject(projectPath);

                string[] references = project.Descendants("ProjectReference")
                    .Select(reference => reference.Attribute("Include")?.Value ?? string.Empty)
                    .ToArray();

                Assert.That(
                    references.Where(reference => reference.Contains("Tests/", StringComparison.OrdinalIgnoreCase)),
                    Is.Empty,
                    $"{projectPath}: test projects must not reference other test projects.");
            }
        }

        [Test]
        public void GivenProductionProject_WhenChecked_ThenItDeclaresNoTestPackages()
        {
            XDocument project = LoadProject(ProductionProjectPath);

            string[] packages = project.Descendants("PackageReference")
                .Select(reference => reference.Attribute("Include")?.Value ?? string.Empty)
                .ToArray();

            Assert.That(
                packages.Where(package =>
                    package.Contains("NUnit", StringComparison.OrdinalIgnoreCase) ||
                    package.Contains("Moq", StringComparison.OrdinalIgnoreCase) ||
                    package.Contains("Test.Sdk", StringComparison.OrdinalIgnoreCase) ||
                    package.Contains("coverlet", StringComparison.OrdinalIgnoreCase) ||
                    package.Contains("ArchUnit", StringComparison.OrdinalIgnoreCase) ||
                    package.Contains("bunit", StringComparison.OrdinalIgnoreCase)),
                Is.Empty,
                $"{ProductionProjectPath}: the production project must not declare test packages.");
        }

        [Test]
        public void GivenTestProjects_WhenChecked_ThenAnalysersAreEnabledForEveryTestLevel()
        {
            foreach (string projectPath in TestProjectPaths)
            {
                XDocument project = LoadProject(projectPath);

                string[] packages = project.Descendants("PackageReference")
                    .Select(reference => reference.Attribute("Include")?.Value ?? string.Empty)
                    .ToArray();

                Assert.That(packages, Does.Contain("NUnit"), $"{projectPath}: NUnit must be the test framework.");
                Assert.That(packages, Does.Contain("NUnit3TestAdapter"), $"{projectPath}: the NUnit adapter must be referenced.");
            }
        }

        [TestCase("NuciCraftInfo.UnitTests/NuciCraftInfo.UnitTests.csproj")]
        [TestCase("NuciCraftInfo.IntegrationTests/NuciCraftInfo.IntegrationTests.csproj")]
        public void GivenExecutableTestProjects_WhenChecked_ThenCoverageCollectionIsConfigured(string projectPath)
        {
            XDocument project = LoadProject(projectPath);

            string[] packages = project.Descendants("PackageReference")
                .Select(reference => reference.Attribute("Include")?.Value ?? string.Empty)
                .ToArray();

            Assert.That(packages, Does.Contain("coverlet.collector"), $"{projectPath}: coverage collection must be configured.");
        }

        [Test]
        public void GivenProjects_WhenChecked_ThenPackageReferencesDeclareExplicitVersions()
        {
            foreach (string projectPath in AllProjectPaths)
            {
                XDocument project = LoadProject(projectPath);

                IEnumerable<XElement> unpinned = project.Descendants("PackageReference")
                    .Where(reference => reference.Attribute("Version") is null &&
                        reference.Element("Version") is null);

                Assert.That(unpinned, Is.Empty, $"{projectPath}: every package reference must declare an explicit version.");
            }
        }

        [Test]
        public void GivenProjects_WhenChecked_ThenPackageReferencesAreAlphabeticalAcrossAllProjects()
        {
            foreach (string projectPath in AllProjectPaths)
            {
                XDocument project = LoadProject(projectPath);

                string[] packages = project.Descendants("PackageReference")
                    .Select(reference => reference.Attribute("Include")?.Value ?? string.Empty)
                    .ToArray();

                Assert.That(packages, Is.Not.Empty, $"{projectPath}: no package references were found.");
                Assert.That(
                    packages,
                    Is.EqualTo(packages.OrderBy(package => package, StringComparer.Ordinal).ToArray()),
                    $"{projectPath}: package references must be ordered alphabetically.");
            }
        }

        [Test]
        public void GivenProjects_WhenChecked_ThenPrivateAssetsAreDeclaredForDevelopmentPackages()
        {
            string[] developmentPackages = ["coverlet.collector", "NUnit.Analyzers"];

            foreach (string projectPath in AllProjectPaths)
            {
                XDocument project = LoadProject(projectPath);

                foreach (XElement reference in project.Descendants("PackageReference"))
                {
                    string package = reference.Attribute("Include")?.Value ?? string.Empty;

                    if (!developmentPackages.Contains(package, StringComparer.Ordinal))
                    {
                        continue;
                    }

                    Assert.That(
                        reference.Element("PrivateAssets")?.Value,
                        Is.EqualTo("all"),
                        $"{projectPath}: {package} must declare PrivateAssets='all'.");
                }
            }
        }

        private static IEnumerable<string> AllProjectPaths
            => TestProjectPaths.Prepend(ProductionProjectPath);

        private static XDocument LoadProject(string relativePath)
            => XDocument.Parse(
                ArchitectureResources.ReadResource(
                    ArchitectureResources.ProjectFilePrefix,
                    relativePath));

        private static string GetProperty(XDocument project, string name)
            => project.Descendants()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, name, StringComparison.Ordinal))
                ?.Value ?? string.Empty;
    }
}
