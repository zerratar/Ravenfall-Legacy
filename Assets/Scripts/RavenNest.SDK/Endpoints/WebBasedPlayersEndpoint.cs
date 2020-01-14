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

        public Task<RavenNest.Models.Player> PlayerJoinAsync(string userId, string username)
        {
            return request.Create()
                .Identifier(userId)
                .AddParameter("value", username)
                .Build()
                .SendAsync<RavenNest.Models.Player>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Post);
        }

        public Task<RavenNest.Models.Player> GetPlayerAsync(string userId)
        {
            return request.Create()
                .Identifier(userId)
                .Build()
                .SendAsync<RavenNest.Models.Player>(
                    ApiRequestTarget.Players,
                    ApiRequestType.Get);
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

        public Task<bool> UnEquipItemAsync(string userId, Guid item)
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

        public Task<bool> EquipItemAsync(string userId, Guid item)
        {
            return request.Create()
                .Identifier(userId)
                .Method("equip")
                .AddParameter(item.ToString())
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Players,
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

        public Task<bool> UpdateExperienceAsync(string userId, decimal[] experience)
        {
            return request.Create()
                .Identifier(userId)
                .Method("experience")
                .AddParameter("values", experience)
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

        public Task<int> GiftItemAsync(string userId, string receiverUserId, Guid itemId, int amount)
        {
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
            return request.Create()
                .Method("update")
                .AddParameter("values", states)
                .Build()
                .SendAsync<bool[]>(ApiRequestTarget.Players, ApiRequestType.Post);
        }
    }
}