using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using NUnit.Framework;

using NuciCraftInfo.IntegrationTests.Infrastructure;

namespace NuciCraftInfo.IntegrationTests.Endpoints
{
    [TestFixture]
    public sealed class ServerEndpointTests
    {
        private const string ServerEndpoint = "/api/nucicraft/server";
        private const string ServerResource = "Server";

        private NuciCraftInfoFactory factory = null!;
        private HttpClient client = null!;

        [SetUp]
        public void SetUp()
        {
            factory = new NuciCraftInfoFactory();
            client = factory.CreateClient();
        }

        [TearDown]
        public void TearDown()
        {
            client.Dispose();
            factory.Dispose();
        }

        [Test]
        public async Task GivenTheServerEndpoint_WhenTheApiReturnsTheServer_ThenTheContentIsProxied()
        {
            factory.NuciCraftHandler.EnqueueJsonResponse(
                ServerResource,
                """{"success":true,"message":"Operation completed successfully.","code":"SUCCESS","hmac":null,"content":{"name":"NuciCraft","hostname":"mc.nucilandia.ro","javaEditionPort":25565,"bedrockEditionPort":19132,"onlinePlayersCount":12}}""");

            using HttpResponseMessage response = await client.GetAsync(ServerEndpoint);
            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
                Assert.That(document.RootElement.GetProperty("hostname").GetString(), Is.EqualTo("mc.nucilandia.ro"));
                Assert.That(document.RootElement.GetProperty("onlinePlayersCount").GetInt32(), Is.EqualTo(12));
                Assert.That(document.RootElement.TryGetProperty("content", out JsonElement _), Is.False);
                Assert.That(factory.NuciCraftHandler.RequestedResources, Does.Contain(ServerResource));
            });
        }

        [Test]
        public async Task GivenTheServerEndpoint_WhenRequestingIt_ThenTheApiKeyIsSentAsABearerToken()
        {
            factory.NuciCraftHandler.EnqueueJsonResponse(
                ServerResource,
                """{"success":true,"message":"Operation completed successfully.","code":"SUCCESS","hmac":null,"content":{}}""");

            using HttpResponseMessage response = await client.GetAsync(ServerEndpoint);

            Assert.That(
                factory.NuciCraftHandler.RequestedAuthorisations,
                Does.Contain($"Bearer {NuciCraftInfoFactory.ApiKey}"));
        }

        [Test]
        public async Task GivenTheServerEndpoint_WhenRequestingIt_ThenTheApiMetadataHeadersAreSent()
        {
            factory.NuciCraftHandler.EnqueueJsonResponse(
                ServerResource,
                """{"success":true,"message":"Operation completed successfully.","code":"SUCCESS","hmac":null,"content":{}}""");

            using HttpResponseMessage response = await client.GetAsync(ServerEndpoint);

            HttpRequestMessage request = factory.NuciCraftHandler.RequestedMessages[0];

            Assert.Multiple(() =>
            {
                Assert.That(request.Headers.GetValues("X-Client-ID"), Has.Exactly(1).Items);
                Assert.That(request.Headers.GetValues("X-Request-ID"), Has.Exactly(1).Items);
                Assert.That(request.Headers.GetValues("X-Timestamp"), Has.Exactly(1).Items);
            });
        }

        [Test]
        public async Task GivenTheServerEndpoint_WhenTheApiReturnsNotFound_ThenAnEmptyPayloadIsReturned()
        {
            factory.NuciCraftHandler.EnqueueErrorResponse(
                ServerResource,
                HttpStatusCode.NotFound,
                """{"success":false,"message":"The requested resource was not found.","code":"NOT_FOUND","hmac":null}""");

            using HttpResponseMessage response = await client.GetAsync(ServerEndpoint);
            string responseBody = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(responseBody, Is.Empty);
            });
        }

        [TestCase(HttpStatusCode.InternalServerError)]
        [TestCase(HttpStatusCode.Forbidden)]
        [TestCase(HttpStatusCode.Unauthorized)]
        public async Task GivenTheServerEndpoint_WhenTheApiFails_ThenServiceUnavailableIsReturned(HttpStatusCode apiStatusCode)
        {
            factory.NuciCraftHandler.EnqueueResponse(ServerResource, apiStatusCode);

            using HttpResponseMessage response = await client.GetAsync(ServerEndpoint);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        }

        [Test]
        public async Task GivenTheServerEndpoint_WhenTheApiIsUnreachable_ThenServiceUnavailableIsReturned()
        {
            factory.NuciCraftHandler.EnqueueTransportFailure(ServerResource);

            using HttpResponseMessage response = await client.GetAsync(ServerEndpoint);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        }

        [Test]
        public async Task GivenTheServerEndpoint_WhenTheApiReturnsNoContent_ThenAnEmptyPayloadIsReturned()
        {
            factory.NuciCraftHandler.EnqueueJsonResponse(
                ServerResource,
                """{"success":false,"message":"An error occurred while processing your request.","code":"ERROR","hmac":null}""");

            using HttpResponseMessage response = await client.GetAsync(ServerEndpoint);
            string responseBody = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(responseBody, Is.Empty);
            });
        }

        [Test]
        public async Task GivenTheServerEndpoint_WhenRequestingItTwice_ThenTheApiIsQueriedOnce()
        {
            factory.NuciCraftHandler.EnqueueJsonResponse(
                ServerResource,
                """{"success":true,"message":"Operation completed successfully.","code":"SUCCESS","hmac":null,"content":{"onlinePlayersCount":3}}""");

            using HttpResponseMessage firstResponse = await client.GetAsync(ServerEndpoint);
            using HttpResponseMessage secondResponse = await client.GetAsync(ServerEndpoint);

            Assert.Multiple(() =>
            {
                Assert.That(firstResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(secondResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(
                    factory.NuciCraftHandler.RequestedResources,
                    Has.Exactly(1).EqualTo(ServerResource));
            });
        }
    }
}
