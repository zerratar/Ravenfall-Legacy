using System;
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
        IVillageEndpoint Village { get; }

        //GameEvent PollGameEvent();
        void Update();
        Task<bool> LoginAsync(string username, string password);
        Task<bool> StartSessionAsync(string clientVersion, string accessKey, bool useLocalPlayers);
        Task<bool> EndSessionAsync();
        Task<bool> EndSessionAndRaidAsync(string username, bool war);
        Task<RavenNest.Models.PlayerJoinResult> PlayerJoinAsync(PlayerJoinData data, int retry = 0);
        Task<bool> SavePlayerAsync(PlayerController player);
        Task<bool> SaveTrainingSkill(PlayerController player);
        void SendPlayerLoyaltyData(PlayerController player);
        Guid SessionId { get; }
        string TwitchUserName { get; }
        string TwitchDisplayName { get; }
        string TwitchUserId { get; }

        bool BadClientVersion { get; }
        bool Authenticated { get; }
        bool SessionStarted { get; }
        bool HasActiveRequest { get; }
        bool Desynchronized { get; set; }

        Task<bool> ClearLogoAsync(string twitchUserId);
    }
}