using System.Collections.Generic;

using AngleSharp.Dom;

using Bunit;

using NUnit.Framework;

using NuciCraftInfo.Components.Pages;

namespace NuciCraftInfo.UnitTests.Components.Pages
{
    [TestFixture]
    public sealed class HomeTests
    {
        private static readonly string[] ModalIdentifiers =
        [
            "#modalDiscord", "#modalMinecraftLauncher", "#modalSkins"
        ];

        private BunitContext testContext = null!;

        [SetUp]
        public void SetUp() => testContext = new BunitContext();

        [TearDown]
        public void TearDown() => testContext.Dispose();

        [Test]
        public void GivenTheHomePage_WhenRendering_ThenTheMastheadAndInfoSectionArePresent()
        {
            IRenderedComponent<Home> component = testContext.Render<Home>();

            Assert.Multiple(() =>
            {
                Assert.That(component.Find("#mainNav .navbar-brand").TextContent.Trim(), Is.EqualTo("NuciCraft"));
                Assert.That(component.Find("header.masthead"), Is.Not.Null);
                Assert.That(component.Find("section#info .page-section-heading").TextContent.Trim(), Is.EqualTo("Info"));
            });
        }

        [Test]
        public void GivenTheHomePage_WhenRendering_ThenEveryInformationRowIsListed()
        {
            IRenderedComponent<Home> component = testContext.Render<Home>();

            Assert.That(component.FindAll("#info .row"), Has.Count.EqualTo(7));
        }

        [Test]
        public void GivenTheHomePage_WhenRendering_ThenThePlayerCountStartsUnresolved()
        {
            IRenderedComponent<Home> component = testContext.Render<Home>();

            Assert.That(component.Find("#server-players").TextContent.Trim(), Is.EqualTo("—"));
        }

        [Test]
        public void GivenTheHomePage_WhenRendering_ThenEveryModalIsReachableFromTheInfoSection()
        {
            IRenderedComponent<Home> component = testContext.Render<Home>();

            foreach (string modalIdentifier in ModalIdentifiers)
            {
                Assert.That(
                    component.Find($"#info [data-target='{modalIdentifier}']"),
                    Is.Not.Null);
                Assert.That(
                    component.Find($"{modalIdentifier} .portfolio-modal-title").TextContent.Trim(),
                    Is.Not.Empty);
            }
        }

        [Test]
        public void GivenTheHomePage_WhenRendering_ThenTheDiscordModalLinksToTheInvitation()
        {
            IRenderedComponent<Home> component = testContext.Render<Home>();

            IElement invitationLink = component.Find("#modalDiscord a.btn");

            Assert.That(invitationLink.GetAttribute("href"), Is.EqualTo("/links/discord-invitation"));
        }
    }
}
