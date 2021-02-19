using System;
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    public interface IPlayerEndpoint
    {
        [Obsolete]
        Task<RavenNest.Models.PlayerJoinResult> PlayerJoinAsync(string userId, string username, string identifier);
        Task<RavenNest.Models.PlayerJoinResult> PlayerJoinAsync(PlayerJoinData playerData);
        Task PlayerRemoveAsync(Guid characterId);

        Task<RavenNest.Models.Player> GetPlayerAsync(string userId);

        Task<AddItemResult> AddItemAsync(string userId, Guid item);
        Task<AddItemResult> CraftItemAsync(string userId, Guid item, int amount = 1);

        Task<int> RedeemTokensAsync(string userId, int tokens, bool exact);
        Task<bool> AddTokensAsync(string userId, int tokens);

        Task<int> GetHighscoreAsync(Guid id, string skill);
        Task<bool> EquipItemAsync(string userId, Guid item);
        Task<bool> UnequipItemAsync(string userId, Guid item);
        Task<bool> UnequipAllItemsAsync(string userId);
        Task<bool> EquipBestItemsAsync(string userId);

        Task<bool> ToggleHelmetAsync(string userId);
        Task<bool> UpdateAppearanceAsync(string userId, int[] appearance);
        //Task<bool> UpdateExperienceAsync(string userId, decimal[] experience);

        Task<bool> UpdateStatisticsAsync(string userId, decimal[] statistics);

        Task<bool> UpdateResourcesAsync(string userId, decimal[] resources);

        Task<int> GiftItemAsync(string userId, string receiverUserId, Guid itemId, int amount);
        Task<int> VendorItemAsync(string userId, Guid itemId, int amount);

        Task<bool[]> UpdateManyAsync(PlayerState[] states);
    }
}
