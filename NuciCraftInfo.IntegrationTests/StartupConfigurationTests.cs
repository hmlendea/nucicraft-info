using System;

using NUnit.Framework;

namespace NuciCraftInfo.IntegrationTests
{
    [TestFixture]
    public sealed class StartupConfigurationTests
    {
        [TestCase("29")]
        [TestCase("0")]
        [TestCase("-30")]
        public void GivenACacheDurationBelowTheMinimum_WhenStarting_ThenTheApplicationRefusesToStart(string cacheDurationInMinutes)
        {
            string[] arguments = [$"--NuciCraftApi:CacheDurationInMinutes={cacheDurationInMinutes}"];

            Assert.That(
                () => Program.Main(arguments),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property(nameof(ArgumentOutOfRangeException.ParamName))
                    .EqualTo("NuciCraftApi:CacheDurationInMinutes"));
        }
    }
}
