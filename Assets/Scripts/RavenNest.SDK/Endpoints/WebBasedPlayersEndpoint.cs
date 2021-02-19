using System;
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{

    internal class WebBasedPlayersEndpoint : IPlayerEndpoint
    {
        private readonly IApiRequestBuilderProvider request;
        private readonly IRavenNestClient client;
        private readonly ILogger logger;

        public WebBasedPlayersEndpoint(
            IRavenNestClient client,
            ILogger logger,
            IApiRequestBuilderProvider request)
        {
            this.client = client;
            this.logger = logger;
            this.request = request;
        }

        public Task PlayerRemoveAsync(Guid characterId)
        {
            if (client.Desynchronized) return Task.CompletedTask;
            return request.Create()
               .AddParameter(characterId.ToString())
                .Build()
               .SendAsync<bool>(
                   ApiRequestTarget.Players,
                   ApiRequestType.Remove);
        }
        public Task<RavenNest.Models.PlayerJoinResult> PlayerJoinAsync(PlayerJoinData playerData)
        {
            if (client.Desynchronized) return null;
            return request.Create()
                .Build()
                .SendAsync<PlayerJoinResult, PlayerJoinData>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Post,
                    playerData);
        }

        [Obsolete]
        public Task<RavenNest.Models.PlayerJoinResult> PlayerJoinAsync(
            string userId, string username, string identifier)
        {
            if (client.Desynchronized) return null;
            return request.Create()
                .Identifier(userId)
                .Method(identifier)
                .AddParameter("value", username)
                .Build()
                .SendAsync<RavenNest.Models.PlayerJoinResult>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Post);
        }

        public Task<RavenNest.Models.Player> GetPlayerAsync(string userId)
        {
            if (client.Desynchronized) return null;
            return request.Create()
                .Identifier(userId)
                .Build()
                .SendAsync<RavenNest.Models.Player>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<AddItemResult> AddItemAsync(string userId, Guid item)
        {
            if (client.Desynchronized) return null;
            return request.Create()
                .Identifier(userId)
                .Method("item")
                .AddParameter(item.ToString())
                .Build()
                .SendAsync<AddItemResult>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<AddItemResult> CraftItemAsync(string userId, Guid item, int amount = 1)
        {
            if (client.Desynchronized) return null;
            return request.Create()
                .Identifier(userId)
                .Method("craft")
                .AddParameter(item.ToString())
                .AddParameter(amount.ToString())
                .Build()
                .SendAsync<AddItemResult>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<int> RedeemTokensAsync(string userId, int amount, bool exact)
        {
            if (client.Desynchronized) return null;
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
            if (client.Desynchronized) return null;
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
            if (client.Desynchronized) return null;
            return request.Create()
                .Identifier(userId)
                .Method("unequip")
                .AddParameter(item.ToString())
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<bool> UnequipAllItemsAsync(string userId)
        {
            if (client.Desynchronized) return null;
            return request.Create()
                .Identifier(userId)
                .Method("unequipall")
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<bool> EquipItemAsync(string userId, Guid item)
        {
            if (client.Desynchronized) return null;
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
            if (client.Desynchronized) return null;
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
            if (client.Desynchronized) return null;
            return request.Create()
                .Identifier(userId)
                .Method("toggle-helmet")
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Players,
                    ApiRequestType.Get);
        }

        public Task<bool> UpdateSyntyAppearanceAsync(string userId, SyntyAppearance appearance)
        {
            if (client.Desynchronized) return null;
            return request.Create()
                .Identifier(userId)
                .Method("appearance")
                .AddParameter("values", appearance)
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Players, ApiRequestType.Post);
        }

        public Task<bool> UpdateAppearanceAsync(string userId, int[] appearance)
        {
            if (client.Desynchronized) return null;
            return request.Create()
                .Identifier(userId)
                .Method("appearance")
                .AddParameter("values", appearance)
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Players, ApiRequestType.Post);
        }

        public Task<bool> UpdateStatisticsAsync(string userId, decimal[] statistics)
        {
            if (client.Desynchronized) return null;
            return request.Create()
                .Identifier(userId)
                .Method("statistics")
                .AddParameter("values", statistics)
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Players, ApiRequestType.Post);
        }

        public Task<bool> UpdateResourcesAsync(string userId, decimal[] resources)
        {
            if (client.Desynchronized) return null;
            return request.Create()
                .Identifier(userId)
                .Method("resources")
                .AddParameter("values", resources)
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Players, ApiRequestType.Post);
        }

        public Task<int> GiftItemAsync(string userId, string receiverUserId, Guid itemId, int amount)
        {
            if (client.Desynchronized) return null;
            return request.Create()
                .Identifier(userId)
                .Method("gift")
                .AddParameter(receiverUserId)
                .AddParameter(itemId.ToString())
                .AddParameter(amount.ToString())
                .Build()
                .SendAsync<int>(ApiRequestTarget.Players, ApiRequestType.Get);
        }
        public Task<int> VendorItemAsync(string userId, Guid itemId, int amount)
        {
            if (client.Desynchronized) return null;
            return request.Create()
                .Identifier(userId)
                .Method("vendor")
                .AddParameter(itemId.ToString())
                .AddParameter(amount.ToString())
                .Build()
                .SendAsync<int>(ApiRequestTarget.Players, ApiRequestType.Get);
        }

        public Task<bool[]> UpdateManyAsync(PlayerState[] states)
        {
            if (client.Desynchronized) return null;
            return request.Create()
                .Method("update")
                .AddParameter("values", states)
                .Build()
                .SendAsync<bool[]>(ApiRequestTarget.Players, ApiRequestType.Post);
        }

        public Task<int> GetHighscoreAsync(Guid id, string skill)
        {
            if (client.Desynchronized) return null;
            return request.Create()
               .Method("highscore")
               .AddParameter(id.ToString())
               .AddParameter(skill)
               .Build()
               .SendAsync<int>(ApiRequestTarget.Players, ApiRequestType.Get);
        }
    }
}