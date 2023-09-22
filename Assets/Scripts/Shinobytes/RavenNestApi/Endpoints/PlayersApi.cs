using System;
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{

    public class PlayersApi
    {
        private readonly IApiRequestBuilderProvider request;
        private readonly RavenNestClient client;
        private readonly ILogger logger;

        public PlayersApi(
            RavenNestClient client,
            ILogger logger,
            IApiRequestBuilderProvider request)
        {
            this.client = client;
            this.logger = logger;
            this.request = request;
        }

        public Task PlayerRemoveAsync(Guid characterId)
        {
            return request.Create()
               .AddParameter(characterId.ToString())
                .Build()
               .SendAsync<bool>(
                   ApiRequestTarget.Players,
                   ApiRequestType.Remove);
        }

        public Task<RavenNest.Models.PlayerJoinResult> PlayerJoinAsync(PlayerJoinData playerData)
        {
            return request.Create()
                .Build()
                .SendAsync<PlayerJoinResult, PlayerJoinData>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Post,
                    playerData);
        }

        public Task<RavenNest.Models.PlayerRestoreResult> RestoreAsync(PlayerRestoreData playerData)
        {
            return request.Create()
                .Method("restore")
                .Build()
                .SendAsync<PlayerRestoreResult, PlayerRestoreData>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Post,
                    playerData);
        }

        public Task<AddItemInstanceResult> AddItemAsync(Guid characterId, GameInventoryItem item)
        {
            return request.Create()
                .Identifier("v2/" + characterId)
                .Method("item")
                .Build()
                .SendAsync<AddItemInstanceResult, InventoryItem>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Post,
                    item.InventoryItem);
        }

        public Task<AddItemResult> AddItemAsync(Guid characterId, Guid item)
        {
            return request.Create()
                .Identifier("v2/" + characterId)
                .Method("item")
                .AddParameter(item.ToString())
                .Build()
                .SendAsync<AddItemResult>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<ItemProductionResult> ProduceItemAsync(Guid characterId, Guid recipeId, long amount)
        {
            return request.Create()
                .Identifier(characterId.ToString())
                .Method("produce")
                .AddParameter(recipeId.ToString())
                .AddParameter(amount.ToString())
                .Build()
                .SendAsync<ItemProductionResult>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<CraftItemResult> CraftItemsAsync(Guid characterId, Guid item, int amount = 1)
        {
            return request.Create()
                .Identifier("v2/" + characterId)
                .Method("craft")
                .AddParameter(item.ToString())
                .AddParameter(amount.ToString())
                .Build()
                .SendAsync<CraftItemResult>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<RedeemItemResult> RedeemItemAsync(Guid characterId, Guid itemId)
        {
            return request.Create()
                .Identifier(characterId.ToString())
                .Method("redeem-item")
                .AddParameter(itemId.ToString())
                .Build()
                .SendAsync<RedeemItemResult>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<bool> AddTokensAsync(Guid characterId, int amount)
        {
            return request.Create()
                .Identifier("v2/" + characterId)
                .Method("add-tokens")
                .AddParameter(amount.ToString())
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<ItemEnchantmentResult> EnchantInventoryItemAsync(Guid characterId, Guid inventoryItem)
        {
            return request.Create()
                .Identifier("v2/" + characterId)
                .Method("enchant-instance")
                .AddParameter(inventoryItem.ToString())
                .Build()
                .SendAsync<ItemEnchantmentResult>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        internal Task<int> GetAutoJoinDungeonCostAsync()
        {
            return request.Create().Method("dungeon-auto-cost").Build().SendAsync<int>(ApiRequestTarget.Players, ApiRequestType.Get);
        }

        public Task<int> GetAutoJoinRaidCostAsync()
        {
            return request.Create().Method("raid-auto-cost").Build().SendAsync<int>(ApiRequestTarget.Players, ApiRequestType.Get);
        }

        public Task<ItemUseResult> UseItemAsync(Guid characterId, Guid inventoryItemId)
        {
            return request.Create()
                .Identifier(characterId.ToString())
                .Method("use")
                .AddParameter(inventoryItemId.ToString())
                .Build()
                .SendAsync<ItemUseResult>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get, true);
        }


        public Task<ItemUseResult> UseItemAsync(Guid characterId, Guid inventoryItemId, string arg)
        {
            return request.Create()
                .Identifier(characterId.ToString())
                .Method("use")
                .AddParameter(inventoryItemId.ToString())
                .AddParameter(arg)
                .Build()
                .SendAsync<ItemUseResult>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get, true);
        }

        public Task<bool> AutoJoinDungeon(Guid characterId)
        {
            return request.Create()
                .Identifier("" + characterId)
                .Method("dungeon-auto")
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get, true);
        }

        public Task<bool> AutoJoinRaid(Guid characterId)
        {
            return request.Create()
                .Identifier("" + characterId)
                .Method("raid-auto")
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get, true);
        }

        public Task<bool[]> AutoJoinRaid(Guid[] characterId)
        {
            return request.Create()
                .Method("raid-auto")
                .AddParameter("values", characterId)
                .Build()
                .SendAsync<bool[]>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Post, true);
        }

        public Task<bool[]> AutoJoinDungeon(Guid[] characterId)
        {
            return request.Create()
                .Method("dungeon-auto")
                .AddParameter("values", characterId)
                .Build()
                .SendAsync<bool[]>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Post, true);
        }


        public Task<ItemEnchantmentResult> DisenchantInventoryItemAsync(Guid characterId, Guid inventoryItem)
        {
            return request.Create()
                .Identifier("v2/" + characterId)
                .Method("disenchant-instance")
                .AddParameter(inventoryItem.ToString())
                .Build()
                .SendAsync<ItemEnchantmentResult>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<ClearEnchantmentCooldownResult> ClearEnchantmentCooldownAsync(Guid characterId)
        {
            return request.Create()
                .Identifier("" + characterId)
                .Method("clear-enchantment-cooldown")
                .Build()
                .SendAsync<ClearEnchantmentCooldownResult>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }


        public Task<EnchantmentCooldownResult> GetEnchantmentCooldownAsync(Guid characterId)
        {
            return request.Create()
                .Identifier("" + characterId)
                .Method("enchantment-cooldown")
                .Build()
                .SendAsync<EnchantmentCooldownResult>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }


        public Task<bool> UnequipInventoryItemAsync(Guid characterId, Guid inventoryItem)
        {
            return request.Create()
                .Identifier("v2/" + characterId)
                .Method("unequip-instance")
                .AddParameter(inventoryItem.ToString())
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<bool> UnequipAllItemsAsync(Guid characterId)
        {
            return request.Create()
                .Identifier("v2/" + characterId)
                .Method("unequipall")
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<bool> EquipInventoryItemAsync(Guid characterId, Guid inventoryItem)
        {
            return request.Create()
                .Identifier("v2/" + characterId)
                .Method("equip-instance")
                .AddParameter(inventoryItem.ToString())
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        [Obsolete("Use EquipInventoryItemAsync instead")]
        public Task<bool> EquipItemAsync(Guid characterId, Guid item)
        {
            return request.Create()
                .Identifier("v2/" + characterId)
                .Method("equip")
                .AddParameter(item.ToString())
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<bool> EquipBestItemsAsync(Guid characterId)
        {
            return request.Create()
                .Identifier("v2/" + characterId)
                .Method("equipall")
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<bool> ToggleHelmetAsync(Guid characterId)
        {
            return request.Create()
                .Identifier("v2/" + characterId)
                .Method("toggle-helmet")
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<bool> UpdateAppearanceAsync(Guid characterId, int[] appearance)
        {
            return request.Create()
                .Identifier("v2/" + characterId)
                .Method("appearance")
                .AddParameter("values", appearance)
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Players, ApiRequestType.Post);
        }

        public Task<long> GiftItemAsync(Guid characterId, Guid receiverUserId, Guid inventoryItemId, long amount)
        {
            return request.Create()
                .Identifier("v2/" + characterId)
                .Method("gift")
                .AddParameter(receiverUserId.ToString())
                .AddParameter(inventoryItemId.ToString())
                .AddParameter(amount.ToString())
                .Build()
                .SendAsync<long>(ApiRequestTarget.Players, ApiRequestType.Get);
        }

        public Task<long> VendorInventoryItemAsync(Guid characterId, Guid inventoryItemId, long amount)
        {
            return request.Create()
                .Identifier("v2/" + characterId)
                .Method("vendor-instance")
                .AddParameter(inventoryItemId.ToString())
                .AddParameter(amount.ToString())
                .Build()
                .SendAsync<long>(ApiRequestTarget.Players, ApiRequestType.Get);
        }

        public Task<int> GetHighscoreAsync(Guid id, string skill)
        {
            return request.Create()
               .Method("highscore")
               .AddParameter(id.ToString())
               .AddParameter(skill)
               .Build()
               .SendAsync<int>(ApiRequestTarget.Players, ApiRequestType.Get);
        }

        public Task<bool> SendLoyaltyUpdateAsync(LoyaltyUpdate req)
        {
            return request.Create()
               .Method("loyalty")
               .Build()
               .SendAsync<bool>(ApiRequestTarget.Players, ApiRequestType.Post, req);
        }

    }
}