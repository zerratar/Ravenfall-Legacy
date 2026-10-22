using System;
using System.Threading.Tasks;
using RavenNest.Models;
using RavenNest.Protocol;

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

        public Task<bool> PlayerRemoveFailedAsync(Guid characterId, string reason)
        {
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<bool>(Commands.PlayerRemoveFailed, CommandArgs.Pack(characterId, reason));
            }

            var req = request.Create()
                .AddParameter(characterId)
                .Method("remove-failed")
                .Build();

            return req.SendAsync<bool, string>(
                ApiRequestTarget.Players,
                ApiRequestType.Post,
                reason);
        }

        public Task PlayerRemoveAsync(Guid characterId)
        {
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<bool>(Commands.PlayerRemove, CommandArgs.Pack(characterId));
            }

            return request.Create()
               .AddParameter(characterId)
                .Build()
               .SendAsync<bool>(
                   ApiRequestTarget.Players,
                   ApiRequestType.Remove);
        }

        public Task<RavenNest.Models.PlayerJoinResult> PlayerJoinAsync(PlayerJoinData playerData)
        {
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<PlayerJoinResult>(Commands.PlayerJoin, CommandArgs.Pack(playerData));
            }

            return request.Create()
                .Build()
                .SendAsync<PlayerJoinResult, PlayerJoinData>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Post,
                    playerData);
        }

        public Task<RavenNest.Models.PlayerRestoreResult> RestoreAsync(PlayerRestoreData playerData)
        {
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<PlayerRestoreResult>(Commands.PlayerRestore, CommandArgs.Pack(playerData));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<AddItemInstanceResult>(Commands.AddItemInstance, CommandArgs.Pack(characterId, item.InventoryItem));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<AddItemResult>(Commands.AddItem, CommandArgs.Pack(characterId, item));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<ItemProductionResult>(Commands.Produce, CommandArgs.Pack(characterId, recipeId, amount));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<CraftItemResult>(Commands.Craft, CommandArgs.Pack(characterId, item, amount));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<RedeemItemResult>(Commands.Redeem, CommandArgs.Pack(characterId, itemId));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<bool>(Commands.AddTokens, CommandArgs.Pack(characterId, amount));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<ItemEnchantmentResult>(Commands.Enchant, CommandArgs.Pack(characterId, inventoryItem));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<int>(Commands.AutoJoinRaidCost, CommandArgs.Pack());
            }

            return request.Create().Method("raid-auto-cost").Build().SendAsync<int>(ApiRequestTarget.Players, ApiRequestType.Get);
        }

        public Task<ItemUseResult> UseItemAsync(Guid characterId, Guid inventoryItemId)
        {
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<ItemUseResult>(Commands.UseItem, CommandArgs.Pack(characterId, inventoryItemId, (string)null));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<ItemUseResult>(Commands.UseItem, CommandArgs.Pack(characterId, inventoryItemId, arg));
            }

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
            if (client.UseV2)
            {
                return FirstOf(client.V2.Commands.CallAsync<bool[]>(Commands.AutoJoinDungeon, CommandArgs.Pack(new[] { characterId })));
            }

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
            if (client.UseV2)
            {
                return FirstOf(client.V2.Commands.CallAsync<bool[]>(Commands.AutoJoinRaid, CommandArgs.Pack(new[] { characterId })));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<bool[]>(Commands.AutoJoinRaid, CommandArgs.Pack(characterId));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<bool[]>(Commands.AutoJoinDungeon, CommandArgs.Pack(characterId));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<ItemEnchantmentResult>(Commands.Disenchant, CommandArgs.Pack(characterId, inventoryItem));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<ClearEnchantmentCooldownResult>(Commands.ClearEnchantmentCooldown, CommandArgs.Pack(characterId));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<EnchantmentCooldownResult>(Commands.EnchantmentCooldown, CommandArgs.Pack(characterId));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<bool>(Commands.Unequip, CommandArgs.Pack(characterId, inventoryItem));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<bool>(Commands.UnequipAll, CommandArgs.Pack(characterId));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<bool>(Commands.Equip, CommandArgs.Pack(characterId, inventoryItem));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<bool>(Commands.EquipItem, CommandArgs.Pack(characterId, item));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<bool>(Commands.EquipBest, CommandArgs.Pack(characterId));
            }

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
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<bool>(Commands.ToggleHelmet, CommandArgs.Pack(characterId));
            }

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

        public Task<GiftItemResult> GiftItemAsync(Guid characterId, Guid receiverUserId, Guid inventoryItemId, long amount)
        {
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<GiftItemResult>(Commands.Gift, CommandArgs.Pack(characterId, receiverUserId, inventoryItemId, amount));
            }

            return request.Create()
                .Identifier("v3/" + characterId)
                .Method("gift")
                .AddParameter(receiverUserId.ToString())
                .AddParameter(inventoryItemId.ToString())
                .AddParameter(amount.ToString())
                .Build()
                .SendAsync<GiftItemResult>(ApiRequestTarget.Players, ApiRequestType.Get);
        }

        /// <summary>
        /// Sends coins from one character to another
        /// </summary>
        /// <param name="senderCharacterId"></param>
        /// <param name="receiverCharacterId"></param>
        /// <param name="amount"></param>
        /// <returns>The amount that was transfered. Returns -1 if amount is <= 0 or characters does not exist. -2 if coins are 0</returns>
        public Task<long> SendCoinsAsync(Guid senderCharacterId, Guid receiverCharacterId, long amount)
        {
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<long>(Commands.SendCoins, CommandArgs.Pack(senderCharacterId, receiverCharacterId, amount));
            }

            return request.Create()
                .Identifier(senderCharacterId.ToString())
                .Method("send-coins")
                .AddParameter(receiverCharacterId.ToString())
                .AddParameter(amount.ToString())
                .Build()
                .SendAsync<long>(ApiRequestTarget.Players, ApiRequestType.Get);
        }

        public Task<GiftItemResult> SendItemAsync(Guid characterId, string alias, Guid inventoryItemId, long amount)
        {
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<GiftItemResult>(Commands.SendItem, CommandArgs.Pack(characterId, alias, inventoryItemId, amount));
            }

            return request.Create()
                .Identifier(characterId.ToString())
                .Method("send")
                .AddParameter(alias)
                .AddParameter(inventoryItemId.ToString())
                .AddParameter(amount.ToString())
                .Build()
                .SendAsync<GiftItemResult>(ApiRequestTarget.Players, ApiRequestType.Get);
        }

        public Task<long> VendorInventoryItemAsync(Guid characterId, Guid inventoryItemId, long amount)
        {
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<long>(Commands.Vendor, CommandArgs.Pack(characterId, inventoryItemId, amount));
            }

            return request.Create()
                .Identifier("v2/" + characterId)
                .Method("vendor-instance")
                .AddParameter(inventoryItemId.ToString())
                .AddParameter(amount.ToString())
                .Build()
                .SendAsync<long>(ApiRequestTarget.Players, ApiRequestType.Get);
        }

        /// <summary>The character as the server has it now, inventory included (used to repair a missed inventory change).</summary>
        public Task<RavenNest.Models.Player> GetPlayerAsync(Guid characterId)
        {
            return request.Create()
                .Identifier(characterId.ToString())
                .Build()
                .SendAsync<RavenNest.Models.Player>(ApiRequestTarget.Players, ApiRequestType.Get);
        }

        public Task<int> GetHighscoreAsync(Guid id, string skill)
        {
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<int>(Commands.Highscore, CommandArgs.Pack(id, skill));
            }

            return request.Create()
               .Method("highscore")
               .AddParameter(id.ToString())
               .AddParameter(skill)
               .Build()
               .SendAsync<int>(ApiRequestTarget.Players, ApiRequestType.Get);
        }

        public Task<bool> SendLoyaltyUpdateAsync(LoyaltyUpdate req)
        {
            if (client.UseV2)
            {
                return client.V2.Commands.CallAsync<bool>(Commands.LoyaltyUpdate, CommandArgs.Pack(req));
            }

            return request.Create()
               .Method("loyalty")
               .Build()
               .SendAsync<bool>(ApiRequestTarget.Players, ApiRequestType.Post, req);
        }


        private static async Task<bool> FirstOf(Task<bool[]> all)
        {
            var result = await all;
            return result != null && result.Length > 0 && result[0];
        }
    }
}