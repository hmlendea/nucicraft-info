using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using NUnit.Framework;

namespace NuciCraftInfo.ArchitectureTests
{
    [TestFixture]
    public sealed class SolutionConventionTests
    {
        private static string SolutionPath => "NuciCraftInfo.slnx";

        [Test]
        public void GivenSolution_WhenLoaded_ThenItUsesTheSdkStyleFormat()
        {
            string content = ReadSolution();
            XDocument solution = XDocument.Parse(content);

            Assert.That(solution.Root, Is.Not.Null, "The solution file must have a root element.");
            Assert.That(solution.Root!.Name.LocalName, Is.EqualTo("Solution"), "The solution must use the SDK-style root element.");
            Assert.That(solution.Root.Attributes(), Is.Empty, "The solution root element must not declare attributes.");
            Assert.That(content.TrimStart(), Does.Not.StartWith("<?xml"), "The solution must not declare an XML declaration.");
        }

        [Test]
        public void GivenSolution_WhenLoaded_ThenItContainsNoVersionElement()
            => Assert.That(
                XDocument.Parse(ReadSolution()).Descendants().Where(element =>
                    string.Equals(element.Name.LocalName, "Version", StringComparison.Ordinal)),
                Is.Empty,
                "The solution must not contain a Version element.");

        [Test]
        public void GivenSolution_WhenLoaded_ThenEveryProjectDeclaresOnlyItsPath()
        {
            XElement[] projects = GetProjects();

            Assert.That(projects, Is.Not.Empty, "The solution must declare projects.");

            foreach (XElement project in projects)
            {
                Assert.That(
                    project.Attributes().Select(attribute => attribute.Name.LocalName),
                    Is.EqualTo(new[] { "Path" }),
                    "Each Project element must declare only the Path attribute.");
            }
        }

        [Test]
        public void GivenSolution_WhenLoaded_ThenTheProjectsAreInTheDocumentedOrder()
            => Assert.That(
                GetProjectPaths(),
                Is.EqualTo(new[]
                {
                    "NuciCraftInfo/NuciCraftInfo.csproj",
                    "NuciCraftInfo.ArchitectureTests/NuciCraftInfo.ArchitectureTests.csproj",
                    "NuciCraftInfo.IntegrationTests/NuciCraftInfo.IntegrationTests.csproj",
                    "NuciCraftInfo.UnitTests/NuciCraftInfo.UnitTests.csproj"
                }));

        [Test]
        public void GivenSolution_WhenLoaded_ThenEveryProjectResidesInASiblingFolderNamedAfterIt()
        {
            IReadOnlyList<string> paths = GetProjectPaths();

            Assert.That(paths, Is.Not.Empty, "The solution must declare projects.");

            foreach (string path in paths)
            {
                string projectName = path.Split('/')[0];

                Assert.That(
                    path,
                    Is.EqualTo($"{projectName}/{projectName}.csproj"),
                    $"The project '{path}' must reside in a sibling folder named after the project, without a 'src' nesting layer.");
            }
        }

        private static XElement[] GetProjects()
            => XDocument.Parse(ReadSolution()).Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "Project", StringComparison.Ordinal))
                .ToArray();

        private static string[] GetProjectPaths()
            => GetProjects()
                .Select(project => project.Attribute("Path")?.Value ?? string.Empty)
                .ToArray();

        private static string ReadSolution()
            => ArchitectureResources.ReadResource(ArchitectureResources.ProjectFilePrefix, SolutionPath);
    }
}