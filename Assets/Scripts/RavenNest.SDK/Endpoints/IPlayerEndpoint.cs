using System;
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    public interface IPlayerEndpoint
    {
        Task<RavenNest.Models.Player> PlayerJoinAsync(string userId, string username);

        Task<RavenNest.Models.Player> GetPlayerAsync(string userId);

        Task<AddItemResult> AddItemAsync(string userId, Guid item);
        Task<AddItemResult> CraftItemAsync(string userId, Guid item);

        Task<bool> UnEquipItemAsync(string userId, Guid item);

        Task<bool> EquipItemAsync(string userId, Guid item);
        Task<bool> ToggleHelmetAsync(string userId);
        Task<bool> UpdateAppearanceAsync(string userId, int[] appearance);
        Task<bool> UpdateExperienceAsync(string userId, decimal[] experience);

        Task<bool> UpdateStatisticsAsync(string userId, decimal[] statistics);

        Task<bool> UpdateResourcesAsync(string userId, decimal[] resources);

        Task<int> GiftItemAsync(string userId, string receiverUserId, Guid itemId, int amount);
        Task<int> VendorItemAsync(string userId, Guid itemId, int amount);

        Task<bool[]> UpdateManyAsync(PlayerState[] states);
    }
}
