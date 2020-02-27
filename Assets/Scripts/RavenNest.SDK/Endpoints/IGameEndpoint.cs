using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    public interface IGameEndpoint
    {
        Task<GameInfo> GetAsync();

        Task<SessionToken> BeginSessionAsync(string clientVersion, string accessKey, bool local, float syncTime);

        Task<bool> EndSessionAndRaidAsync(string username, bool war);

        Task EndSessionAsync();

        Task<EventCollection> PollEventsAsync(int revision);
    }
}