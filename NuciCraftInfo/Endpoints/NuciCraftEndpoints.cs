using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using NuciCraftInfo.Responses;
using NuciCraftInfo.Service;

namespace NuciCraftInfo.Endpoints
{
    public static class NuciCraftEndpoints
    {
        public static string CachePolicyName => "NuciCraft";

        public static void MapNuciCraftEndpoints(this WebApplication application)
        {
            application
                .MapGet("/api/nucicraft/server", async (INuciCraftService service) =>
                {
                    GetServerResponse? server;

                    try
                    {
                        server = await service.GetServerAsync();
                    }
                    catch (HttpRequestException)
                    {
                        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                    }

                    return Results.Json(server);
                })
                .CacheOutput(CachePolicyName);
        }
    }
}
