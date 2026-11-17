using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using NUnit.Framework;

namespace NuciCraftInfo.ArchitectureTests
{
    [TestFixture]
    public sealed class RepositoryConventionTests
    {
        private static string RepositoryRoot
            => Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", ".."));

        [Test]
        public void GivenRepository_WhenLoaded_ThenTheSolutionListsEveryProject()
        {
            XDocument solution = XDocument.Parse(ReadRepositoryFile("NuciCraftInfo.slnx"));

            string[] paths = solution.Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "Project", StringComparison.Ordinal))
                .Select(element => element.Attribute("Path")?.Value ?? string.Empty)
                .ToArray();

            Assert.That(paths, Is.Not.Empty, "The solution must declare projects.");
            Assert.That(paths, Does.Contain("NuciCraftInfo.ArchitectureTests/NuciCraftInfo.ArchitectureTests.csproj"));
            Assert.That(paths, Does.Contain("NuciCraftInfo.UnitTests/NuciCraftInfo.UnitTests.csproj"));
            Assert.That(paths, Does.Contain("NuciCraftInfo.IntegrationTests/NuciCraftInfo.IntegrationTests.csproj"));
        }

        [Test]
        public void GivenRepository_WhenLoaded_ThenTheDotnetWorkflowIsPresent()
        {
            string content = ReadRepositoryFile(".github/workflows/dotnet.yml");

            Assert.That(content, Is.Not.Empty, "The .NET CI workflow must be present.");
            Assert.That(content, Does.Contain("name: .NET"), "The workflow must declare the .NET name.");
            Assert.That(content, Does.Contain("dotnet test"), "The workflow must run the tests.");
        }

        [Test]
        public void GivenRepository_WhenLoaded_ThenTheLicenseIsGplThreeOrLater()
        {
            string license = ReadRepositoryFile("LICENSE");

            Assert.That(license, Is.Not.Empty, "The LICENSE file must be present.");
            Assert.That(license, Does.Contain("GNU GENERAL PUBLIC LICENSE"), "The license must be the GPL.");
            Assert.That(license, Does.Contain("Version 3"), "The license must be version 3.");
        }

        [Test]
        public void GivenRepository_WhenLoaded_ThenTheDocumentationReferencesTheArchitectureTests()
        {
            IReadOnlyList<KeyValuePair<string, string>> documentingFiles =
            [
                new KeyValuePair<string, string>("README.md", "the README"),
                new KeyValuePair<string, string>("ARCHITECTURE.md", "the architecture document")
            ];

            foreach (KeyValuePair<string, string> documentingFile in documentingFiles)
            {
                string content = ReadRepositoryFile(documentingFile.Key);

                Assert.That(content, Is.Not.Empty, $"{documentingFile.Value} must be present.");
                Assert.That(
                    content,
                    Does.Contain("NuciCraftInfo.ArchitectureTests"),
                    $"{documentingFile.Value} must reference the architecture test project.");
                Assert.That(
                    content,
                    Does.Contain("NuciCraftInfo.UnitTests"),
                    $"{documentingFile.Value} must reference the unit test project.");
            }
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            string path = Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

            Assert.That(
                File.Exists(path),
                Is.True,
                $"The repository file '{relativePath}' must be present.");

            return File.ReadAllText(path);
        }
    }
}