using System.Threading;
using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    public interface IWebSocketEndpoint
    {
        bool IsReady { get; }
        Task<bool> UpdateAsync();
        Task<bool> SavePlayerAsync(PlayerController player);
        void Close();
    }
}