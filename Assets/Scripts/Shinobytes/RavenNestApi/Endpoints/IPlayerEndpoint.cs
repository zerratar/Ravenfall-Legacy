using System;
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    public interface IPlayerEndpoint
    {
        //[Obsolete]
        //Task<RavenNest.Models.PlayerJoinResult> PlayerJoinAsync(string userId, string username, string identifier);
        Task<RavenNest.Models.PlayerJoinResult> PlayerJoinAsync(PlayerJoinData playerData);
        Task<RavenNest.Models.PlayerRestoreResult> RestoreAsync(PlayerRestoreData playerData);
        Task PlayerRemoveAsync(Guid characterId);
        Task<RavenNest.Models.Player> GetPlayerAsync(string userId);

        Task<AddItemResult> AddItemAsync(string userId, Guid item);
        Task<Guid> AddItemInstanceAsync(string userId, GameInventoryItem item);
        Task<AddItemResult> CraftItemAsync(string userId, Guid item);
        Task<CraftItemResult> CraftItemsAsync(string userId, Guid item, int amount = 1);
        Task<RedeemItemResult> RedeemItemAsync(Guid characterId, Guid itemId);
        Task<int> RedeemTokensAsync(string userId, int tokens, bool exact);
        Task<bool> AddTokensAsync(string userId, int tokens);
        Task<int> GetHighscoreAsync(Guid id, string skill);
        Task<bool> EquipItemAsync(string userId, Guid item);
        Task<ItemEnchantmentResult> EnchantItemAsync(string userId, Guid inventoryItem);
        Task<bool> EquipItemInstanceAsync(string userId, Guid item);
        Task<bool> UnequipItemInstanceAsync(string userId, Guid inventoryItem);
        Task<bool> UnequipItemAsync(string userId, Guid item);
        Task<bool> UnequipAllItemsAsync(string userId);
        Task<bool> EquipBestItemsAsync(string userId);

        Task<bool> ToggleHelmetAsync(string userId);
        Task<bool> UpdateAppearanceAsync(string userId, int[] appearance);
        //Task<bool> UpdateExperienceAsync(string userId, decimal[] experience);

        Task<bool> UpdateStatisticsAsync(string userId, decimal[] statistics);

        Task<bool> UpdateResourcesAsync(string userId, decimal[] resources);

        Task<long> GiftItemAsync(string userId, string receiverUserId, Guid itemId, long amount);
        Task<long> VendorItemAsync(string userId, Guid itemId, long amount);
        Task<bool> SendLoyaltyUpdateAsync(LoyaltyUpdate req);

        //Task<bool[]> UpdateManyAsync(PlayerState[] states);
    }
}
