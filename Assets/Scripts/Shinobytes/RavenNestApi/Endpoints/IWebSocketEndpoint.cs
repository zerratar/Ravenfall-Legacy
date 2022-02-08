using System;
using System.Threading;
using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    public interface IWebSocketEndpoint
    {
        bool IsReady { get; }
        bool ForceReconnecting { get; }
        Task<bool> UpdateAsync();
        Task<bool> SavePlayerStateAsync(PlayerController player);
        Task<bool> SavePlayerSkillsAsync(PlayerController player);
        Task<bool> SaveActiveSkillAsync(PlayerController player);
        //void SendPlayerLoyaltyData(PlayerController player);
        //void SendPlayerLoyaltyData(TwitchSubscription data);
        //void SendPlayerLoyaltyData(TwitchCheer data);
        void Close();
        void Reconnect();
        Task UpdatePlayerEventStatsAsync(EventTriggerSystem.SysEventStats e);
        void SyncTimeAsync(TimeSpan delta, DateTime localTime, DateTime serverTime);
    }
}