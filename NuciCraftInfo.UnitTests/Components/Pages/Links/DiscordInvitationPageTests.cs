using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using NuciCraftInfo.Components.Pages.Links;

namespace NuciCraftInfo.UnitTests.Components.Pages.Links
{
    [TestFixture]
    public sealed class DiscordInvitationPageTests
    {
        private static string ExpectedInvitationUrl => "https://discord.gg/zFM5xWGF7c";

        private BunitContext testContext = null!;

        [SetUp]
        public void SetUp() => testContext = new BunitContext();

        [TearDown]
        public void TearDown() => testContext.Dispose();

        [Test]
        public void GivenTheDiscordInvitationPage_WhenRendering_ThenTheBrowserIsRedirectedToTheInvitation()
        {
            testContext.Render<DiscordInvitationPage>();

            BunitNavigationManager navigationManager = (BunitNavigationManager)
                testContext.Services.GetRequiredService<NavigationManager>();

            Assert.That(navigationManager.Uri, Is.EqualTo(ExpectedInvitationUrl));
        }

        [Test]
        public void GivenTheDiscordInvitationPage_WhenRendering_ThenAPlaceholderIsDisplayed()
        {
            IRenderedComponent<DiscordInvitationPage> component =
                testContext.Render<DiscordInvitationPage>();

            Assert.That(component.Markup, Does.Contain("Redirecting"));
        }
    }
}
