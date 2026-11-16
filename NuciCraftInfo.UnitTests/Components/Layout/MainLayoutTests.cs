using Bunit;

using Microsoft.AspNetCore.Components.Rendering;

using NUnit.Framework;

using NuciCraftInfo.Components.Layout;

namespace NuciCraftInfo.UnitTests.Components.Layout
{
    [TestFixture]
    public sealed class MainLayoutTests
    {
        private BunitContext testContext = null!;

        [SetUp]
        public void SetUp() => testContext = new BunitContext();

        [TearDown]
        public void TearDown() => testContext.Dispose();

        [Test]
        public void GivenLayoutContent_WhenRenderingTheLayout_ThenTheContentIsRenderedUnchanged()
        {
            IRenderedComponent<MainLayout> component = testContext.Render<MainLayout>(parameters => parameters
                .Add(parameter => parameter.Body, builder =>
                {
                    builder.OpenElement(0, "p");
                    builder.AddContent(1, "Layout content");
                    builder.CloseElement();
                }));

            component.MarkupMatches("<p>Layout content</p>");
        }
    }
}
