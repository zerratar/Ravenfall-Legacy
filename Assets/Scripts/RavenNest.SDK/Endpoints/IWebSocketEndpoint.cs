using System.Threading;
using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    public interface IWebSocketEndpoint
    {
        bool IsReady { get; }
        Task<bool> UpdateAsync();
        Task<bool> SavePlayerStateAsync(PlayerController player);
        Task<bool> SavePlayerSkillsAsync(PlayerController player);
        void Close();
        void Reconnect();
    }
}