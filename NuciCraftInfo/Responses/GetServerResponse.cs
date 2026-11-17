using System.Text.Json.Serialization;

using NuciAPI.Responses;

namespace NuciCraftInfo.Responses
{
    public sealed class GetServerResponse : NuciApiResponseContent
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("hostname")]
        public string Hostname { get; set; } = string.Empty;

        [JsonPropertyName("javaEditionPort")]
        public int JavaEditionPort { get; set; }

        [JsonPropertyName("bedrockEditionPort")]
        public int BedrockEditionPort { get; set; }

        [JsonPropertyName("onlinePlayersCount")]
        public int OnlinePlayersCount { get; set; }
    }
}