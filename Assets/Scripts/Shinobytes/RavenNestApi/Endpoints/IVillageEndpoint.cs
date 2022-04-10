using System;
using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    public interface IVillageEndpoint
    {
        Task<bool> AssignVillageAsync(int type, Guid[] userIds);
        Task<bool> AssignPlayerAsync(int slot, string userId);
        Task<bool> AssignPlayerAsync(int slot, Guid characterId);
        Task<bool> BuildHouseAsync(int slot, int type);
        Task<bool> RemoveHouseAsync(int slot);
        Task<VillageInfo> GetAsync();
    }
}