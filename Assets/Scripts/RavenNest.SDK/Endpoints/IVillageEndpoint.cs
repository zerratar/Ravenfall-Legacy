using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    public interface IVillageEndpoint
    {
        Task<bool> AssignPlayerAsync(int slot, string userId);
        Task<bool> BuildHouseAsync(int slot, int type);
        Task<bool> RemoveHouseAsync(int slot);
    }
}