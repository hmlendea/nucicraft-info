using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using NUnit.Framework;

namespace NuciCraftInfo.ArchitectureTests
{
    [TestFixture]
    public sealed class ProjectConventionTests
    {
        private static string ProductionProjectPath => "NuciCraftInfo/NuciCraftInfo.csproj";

        private static string UnitTestProjectPath => "NuciCraftInfo.UnitTests/NuciCraftInfo.UnitTests.csproj";

        private static string IntegrationTestProjectPath => "NuciCraftInfo.IntegrationTests/NuciCraftInfo.IntegrationTests.csproj";

        private static string ArchitectureTestProjectPath => "NuciCraftInfo.ArchitectureTests/NuciCraftInfo.ArchitectureTests.csproj";

        private static IEnumerable<string> AllProjectPaths =>
        [
            ProductionProjectPath,
            UnitTestProjectPath,
            IntegrationTestProjectPath,
            ArchitectureTestProjectPath
        ];

        [TestCase("NuciCraftInfo/NuciCraftInfo.csproj")]
        [TestCase("NuciCraftInfo.UnitTests/NuciCraftInfo.UnitTests.csproj")]
        [TestCase("NuciCraftInfo.IntegrationTests/NuciCraftInfo.IntegrationTests.csproj")]
        [TestCase("NuciCraftInfo.ArchitectureTests/NuciCraftInfo.ArchitectureTests.csproj")]
        public void GivenProjects_WhenLoaded_ThenTheRequiredPropertiesAreDeclared(string projectPath)
        {
            XDocument project = LoadProject(projectPath);

            Assert.That(GetProperty(project, "TargetFramework"), Is.EqualTo("net10.0"), $"{projectPath}: the target framework must be net10.0.");
            Assert.That(GetProperty(project, "ImplicitUsings"), Is.EqualTo("disable"), $"{projectPath}: implicit usings must be disabled.");
            Assert.That(GetProperty(project, "LangVersion"), Is.EqualTo("latest"), $"{projectPath}: the latest language version must be used.");
            Assert.That(GetProperty(project, "Nullable"), Is.EqualTo("enable"), $"{projectPath}: nullable references must be explicitly enabled.");
            Assert.That(GetProperty(project, "IsPackable"), Is.EqualTo("false"), $"{projectPath}: the project must not be packable.");
        }

        [TestCase("NuciCraftInfo/NuciCraftInfo.csproj")]
        [TestCase("NuciCraftInfo.UnitTests/NuciCraftInfo.UnitTests.csproj")]
        [TestCase("NuciCraftInfo.IntegrationTests/NuciCraftInfo.IntegrationTests.csproj")]
        [TestCase("NuciCraftInfo.ArchitectureTests/NuciCraftInfo.ArchitectureTests.csproj")]
        public void GivenProjects_WhenLoaded_ThenPackageReferencesAreAlphabetical(string projectPath)
        {
            XDocument project = LoadProject(projectPath);

            string[] packages = project.Descendants("PackageReference")
                .Select(reference => GetAttributeValue(reference, "Include"))
                .ToArray();

            Assert.That(packages, Is.Not.Empty, $"{projectPath}: no package references were found.");
            Assert.That(
                packages,
                Is.EqualTo(packages.OrderBy(package => package, StringComparer.Ordinal).ToArray()),
                $"{projectPath}: package references must be ordered alphabetically.");
        }

        [TestCase("NuciCraftInfo.UnitTests/NuciCraftInfo.UnitTests.csproj")]
        [TestCase("NuciCraftInfo.IntegrationTests/NuciCraftInfo.IntegrationTests.csproj")]
        [TestCase("NuciCraftInfo.ArchitectureTests/NuciCraftInfo.ArchitectureTests.csproj")]
        public void GivenTestProjects_WhenLoaded_ThenTheTestFlagsAreDeclared(string projectPath)
        {
            XDocument project = LoadProject(projectPath);

            Assert.That(GetProperty(project, "IsTestProject"), Is.EqualTo("true"), $"{projectPath}: the test project flag must be declared.");
            Assert.That(GetProperty(project, "ExcludeFromCodeCoverage"), Is.EqualTo("true"), $"{projectPath}: the coverage exclusion flag must be declared.");
        }

        [TestCase("NuciCraftInfo.UnitTests/NuciCraftInfo.UnitTests.csproj")]
        [TestCase("NuciCraftInfo.IntegrationTests/NuciCraftInfo.IntegrationTests.csproj")]
        [TestCase("NuciCraftInfo.ArchitectureTests/NuciCraftInfo.ArchitectureTests.csproj")]
        public void GivenTestProjects_WhenLoaded_ThenTheyReferenceTheProductionProject(string projectPath)
        {
            XDocument project = LoadProject(projectPath);

            Assert.That(
                GetProjectReferences(project),
                Has.Some.EqualTo("../NuciCraftInfo/NuciCraftInfo.csproj"),
                $"{projectPath}: the test project must reference the production project.");
        }

        [Test]
        public void GivenProductionProject_WhenLoaded_ThenItDoesNotReferenceTestProjects()
        {
            XDocument project = LoadProject(ProductionProjectPath);

            Assert.That(
                GetProjectReferences(project).Where(reference => reference.Contains("Tests", StringComparison.OrdinalIgnoreCase)),
                Is.Empty,
                "The production project must not reference test projects.");
        }

        [Test]
        public void GivenProductionProject_WhenLoaded_ThenItDeclaresTheDocumentedPackages()
        {
            XDocument project = LoadProject(ProductionProjectPath);

            string[] packages = project.Descendants("PackageReference")
                .Select(reference => GetAttributeValue(reference, "Include"))
                .ToArray();

            Assert.That(packages, Does.Contain("NuciAPI.Client"), "The production project must reference NuciAPI.Client.");
            Assert.That(packages, Does.Contain("NuciWeb.HTTP"), "The production project must reference NuciWeb.HTTP.");
        }

        [Test]
        public void GivenArchitectureTestProject_WhenLoaded_ThenItReferencesTheArchitecturePackages()
        {
            XDocument project = LoadProject(ArchitectureTestProjectPath);

            string[] packages = project.Descendants("PackageReference")
                .Select(reference => GetAttributeValue(reference, "Include"))
                .ToArray();

            Assert.That(packages, Does.Contain("Microsoft.CodeAnalysis.CSharp"), "Roslyn must be available for source rules.");
            Assert.That(packages, Does.Contain("NUnit"), "NUnit must be the test framework.");
            Assert.That(packages, Does.Contain("TngTech.ArchUnitNET.NUnit"), "ArchUnit.NET must drive the compiled rules.");
        }

        [TestCase("NuciCraftInfo/NuciCraftInfo.csproj")]
        [TestCase("NuciCraftInfo.UnitTests/NuciCraftInfo.UnitTests.csproj")]
        [TestCase("NuciCraftInfo.IntegrationTests/NuciCraftInfo.IntegrationTests.csproj")]
        [TestCase("NuciCraftInfo.ArchitectureTests/NuciCraftInfo.ArchitectureTests.csproj")]
        public void GivenProjects_WhenLoaded_ThenNoProjectDeclaresFriendAssembliesOrMultiTargeting(string projectPath)
        {
            XDocument project = LoadProject(projectPath);

            Assert.That(
                project.Descendants().Where(element =>
                    string.Equals(element.Name.LocalName, "InternalsVisibleTo", StringComparison.Ordinal) ||
                    string.Equals(element.Name.LocalName, "TargetFrameworks", StringComparison.Ordinal)),
                Is.Empty,
                $"{projectPath}: InternalsVisibleTo and multi-targeting are not allowed.");
        }

        private static XDocument LoadProject(string relativePath)
            => XDocument.Parse(
                ArchitectureResources.ReadResource(
                    ArchitectureResources.ProjectFilePrefix,
                    relativePath));

        private static string[] GetProjectReferences(XDocument project)
            => project.Descendants("ProjectReference")
                .Select(reference => GetAttributeValue(reference, "Include"))
                .Select(reference => reference.Replace('\\', '/'))
                .ToArray();

        private static string GetAttributeValue(XElement element, string attributeName)
        {
            XAttribute? attribute = element.Attribute(attributeName);

            if (attribute is null)
            {
                return string.Empty;
            }

            return attribute.Value;
        }

        private static string GetProperty(XDocument project, string name)
        {
            XElement? element = project.Descendants()
                .FirstOrDefault(candidate => string.Equals(candidate.Name.LocalName, name, StringComparison.Ordinal));

            if (element is null)
            {
                return string.Empty;
            }

            return element.Value;
        }
    }
}