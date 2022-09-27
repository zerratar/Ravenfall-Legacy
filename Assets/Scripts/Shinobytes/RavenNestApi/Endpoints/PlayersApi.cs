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

        //[Obsolete]
        //public Task<RavenNest.Models.PlayerJoinResult> PlayerJoinAsync(
        //    string userId, string username, string identifier)
        //{
        //    if (client.Desynchronized) return null;
        //    return request.Create()
        //        .Identifier(userId)
        //        .Method(identifier)
        //        .AddParameter("value", username)
        //        .Build()
        //        .SendAsync<RavenNest.Models.PlayerJoinResult>(
        //            ApiRequestTarget.Players,
        //            ApiRequestType.Post);
        //}

        public Task<RavenNest.Models.Player> GetPlayerAsync(string userId)
        {
            return request.Create()
                .Identifier(userId)
                .Build()
                .SendAsync<RavenNest.Models.Player>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<Guid> AddItemInstanceAsync(string userId, GameInventoryItem item)
        {
            return request.Create()
                .Identifier(userId)
                .Method("item-instance")
                .Build()
                .SendAsync<Guid, InventoryItem>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Post,
                    item.InventoryItem);
        }

        public Task<AddItemInstanceResult> AddItemInstanceDetailedAsync(string userId, GameInventoryItem item)
        {
            return request.Create()
                .Identifier(userId)
                .Method("item-instance-detailed")
                .Build()
                .SendAsync<AddItemInstanceResult, InventoryItem>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Post,
                    item.InventoryItem);
        }

        public Task<AddItemResult> AddItemAsync(string userId, Guid item)
        {
            return request.Create()
                .Identifier(userId)
                .Method("item")
                .AddParameter(item.ToString())
                .Build()
                .SendAsync<AddItemResult>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }
        public Task<AddItemResult> CraftItemAsync(string userId, Guid item)
        {
            return request.Create()
                .Identifier(userId)
                .Method("craft")
                .AddParameter(item.ToString())
                .Build()
                .SendAsync<AddItemResult>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }
        public Task<CraftItemResult> CraftItemsAsync(string userId, Guid item, int amount = 1)
        {
            return request.Create()
                .Identifier(userId)
                .Method("craft-many")
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

        public Task<int> RedeemTokensAsync(string userId, int amount, bool exact)
        {
            return request.Create()
                .Identifier(userId)
                .Method("redeem-tokens")
                .AddParameter(amount.ToString())
                .AddParameter(exact.ToString())
                .Build()
                .SendAsync<int>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<bool> AddTokensAsync(string userId, int amount)
        {
            return request.Create()
                .Identifier(userId)
                .Method("add-tokens")
                .AddParameter(amount.ToString())
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }


        public Task<bool> UnequipItemAsync(string userId, Guid item)
        {
            return request.Create()
                .Identifier(userId)
                .Method("unequip")
                .AddParameter(item.ToString())
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<ItemEnchantmentResult> EnchantItemAsync(string userId, Guid inventoryItem)
        {
            return request.Create()
                .Identifier(userId)
                .Method("enchant-item")
                .AddParameter(inventoryItem.ToString())
                .Build()
                .SendAsync<ItemEnchantmentResult>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<bool> UnequipItemInstanceAsync(string userId, Guid inventoryItem)
        {
            return request.Create()
                .Identifier(userId)
                .Method("unequip-instance")
                .AddParameter(inventoryItem.ToString())
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<bool> UnequipAllItemsAsync(string userId)
        {
            return request.Create()
                .Identifier(userId)
                .Method("unequipall")
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<bool> EquipItemInstanceAsync(string userId, Guid inventoryItem)
        {
            return request.Create()
                .Identifier(userId)
                .Method("equip-instance")
                .AddParameter(inventoryItem.ToString())
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<bool> EquipItemAsync(string userId, Guid item)
        {
            return request.Create()
                .Identifier(userId)
                .Method("equip")
                .AddParameter(item.ToString())
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }
        public Task<bool> EquipBestItemsAsync(string userId)
        {
            return request.Create()
                .Identifier(userId)
                .Method("equipall")
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<bool> ToggleHelmetAsync(string userId)
        {
            return request.Create()
                .Identifier(userId)
                .Method("toggle-helmet")
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<bool> UpdateSyntyAppearanceAsync(string userId, SyntyAppearance appearance)
        {
            return request.Create()
                .Identifier(userId)
                .Method("appearance")
                .AddParameter("values", appearance)
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Players, ApiRequestType.Post);
        }

        public Task<bool> UpdateAppearanceAsync(string userId, int[] appearance)
        {
            return request.Create()
                .Identifier(userId)
                .Method("appearance")
                .AddParameter("values", appearance)
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Players, ApiRequestType.Post);
        }

        public Task<bool> UpdateStatisticsAsync(string userId, decimal[] statistics)
        {
            return request.Create()
                .Identifier(userId)
                .Method("statistics")
                .AddParameter("values", statistics)
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Players, ApiRequestType.Post);
        }

        public Task<bool> UpdateResourcesAsync(string userId, decimal[] resources)
        {
            return request.Create()
                .Identifier(userId)
                .Method("resources")
                .AddParameter("values", resources)
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Players, ApiRequestType.Post);
        }

        public Task<long> GiftItemAsync(string userId, string receiverUserId, Guid itemId, long amount)
        {
            return request.Create()
                .Identifier(userId)
                .Method("gift")
                .AddParameter(receiverUserId)
                .AddParameter(itemId.ToString())
                .AddParameter(amount.ToString())
                .Build()
                .SendAsync<long>(ApiRequestTarget.Players, ApiRequestType.Get);
        }
        public Task<long> VendorItemAsync(string userId, Guid itemId, long amount)
        {
            return request.Create()
                .Identifier(userId)
                .Method("vendor")
                .AddParameter(itemId.ToString())
                .AddParameter(amount.ToString())
                .Build()
                .SendAsync<long>(ApiRequestTarget.Players, ApiRequestType.Get);
        }

        public Task<bool[]> UpdateManyAsync(PlayerState[] states)
        {
            return request.Create()
                .Method("update")
                .AddParameter("values", states)
                .Build()
                .SendAsync<bool[]>(ApiRequestTarget.Players, ApiRequestType.Post);
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