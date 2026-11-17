using System;
using System.Collections.Generic;
using System.Net.Http;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using NuciCraftInfo.Components;

namespace NuciCraftInfo.IntegrationTests.Infrastructure
{
    internal sealed class NuciCraftInfoFactory : WebApplicationFactory<App>
    {
        private static string EnvironmentName => "IntegrationTesting";

        internal const string ApiKey = "integration-tests-api-key";

        private const string ApiBaseUrl = "https://test.url.ro/";

        private readonly StubHttpMessageHandler nuciCraftHandler = new();

        internal StubHttpMessageHandler NuciCraftHandler => nuciCraftHandler;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(EnvironmentName);
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["NuciCraftApi:ApiKey"] = ApiKey,
                    ["NuciCraftApi:BaseUrl"] = ApiBaseUrl
                }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<HttpClient>();
                services.AddSingleton(
                    new HttpClient(nuciCraftHandler, disposeHandler: false)
                );
            });
        }
    }
}