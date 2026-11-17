using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using NuciAPI.Client;
using NuciAPI.Responses;

using NuciCraftInfo.Requests;
using NuciCraftInfo.Responses;

namespace NuciCraftInfo.Service
{
    public sealed class NuciCraftService(
        IConfiguration configuration,
        INuciApiClient client) : INuciCraftService
    {
        private static string ServerResource => "Server";

        private static string ClientIdentifier => "NuciCraftInfo";

        public async Task<GetServerResponse?> GetServerAsync()
        {
            NuciApiRequestAuthorisationInfo authorisationInfo = new()
            {
                ClientId = ClientIdentifier,
                BearerToken = configuration.GetValue<string>("NuciCraftApi:ApiKey")
            };

            NuciApiResponse response = await client.SendRequestAsync<
                GetServerRequest,
                NuciApiContentResponse<GetServerResponse>>(
                    HttpMethod.Get,
                    new GetServerRequest(),
                    authorisationInfo,
                    ServerResource
                );

            if (response is NuciApiContentResponse<GetServerResponse> contentResponse)
            {
                return contentResponse.Content;
            }

            if (response is NuciApiErrorResponse errorResponse &&
                string.Equals(errorResponse.Code, NuciApiResponseCodes.ErrorCodes.NotFound))
            {
                return null;
            }

            throw new HttpRequestException(
                $"The NuciCraft API did not return the '{ServerResource}' resource information successfully."
            );
        }
    }
}