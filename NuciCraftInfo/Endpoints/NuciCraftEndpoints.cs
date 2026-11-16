using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NuciCraftInfo.Endpoints
{
    public static class NuciCraftEndpoints
    {
        private const string ServerResource = "Server";

        public static string CachePolicyName => "NuciCraft";

        public static void MapNuciCraftEndpoints(this WebApplication application)
        {
            application
                .MapGet("/api/nucicraft/server", async (HttpClient client, IConfiguration configuration) =>
                {
                    string apiKey = configuration.GetValue<string>("NuciCraftApi:ApiKey") ?? string.Empty;

                    using HttpRequestMessage request = new(HttpMethod.Get, ServerResource);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                    using HttpResponseMessage response = await client.SendAsync(request);

                    if (response.StatusCode is HttpStatusCode.NotFound)
                    {
                        return Results.Json((object?)null);
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                    }

                    using JsonDocument document = JsonDocument.Parse(
                        await response.Content.ReadAsStringAsync()
                    );

                    JsonElement content;

                    if (!document.RootElement.TryGetProperty("content", out content))
                    {
                        return Results.Json((object?)null);
                    }

                    return Results.Json(content.Clone());
                })
                .CacheOutput(CachePolicyName);
        }
    }
}
