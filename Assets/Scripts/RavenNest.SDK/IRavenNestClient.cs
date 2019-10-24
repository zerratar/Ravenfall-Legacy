using System.Threading.Tasks;
using RavenNest.Models;
using RavenNest.SDK.Endpoints;

namespace RavenNest.SDK
{
    public interface IRavenNestClient
    {
        IAuthEndpoint Auth { get; }
        IGameEndpoint Game { get; }
        IItemEndpoint Items { get; }
        IPlayerEndpoint Players { get; }
        IMarketplaceEndpoint Marketplace { get; }
        IWebSocketEndpoint Stream { get; }

        //GameEvent PollGameEvent();
        void Update();
        Task<bool> LoginAsync(string username, string password);
        Task<bool> StartSessionAsync(string clientVersion, string accessKey, bool useLocalPlayers);
        Task<bool> EndSessionAsync();
        Task<bool> EndSessionAndRaidAsync(string username, bool war);
        Task<RavenNest.Models.Player> PlayerJoinAsync(string userId, string username);
        Task<bool> SavePlayerAsync(PlayerController player);

        bool BadClientVersion { get; }

        bool Authenticated { get; }
        bool SessionStarted { get; }
        bool HasActiveRequest { get; }
    }
}