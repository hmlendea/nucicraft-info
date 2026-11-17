using System.Threading.Tasks;

using NuciCraftInfo.Responses;

namespace NuciCraftInfo.Service
{
    public interface INuciCraftService
    {
        public Task<GetServerResponse?> GetServerAsync();
    }
}
