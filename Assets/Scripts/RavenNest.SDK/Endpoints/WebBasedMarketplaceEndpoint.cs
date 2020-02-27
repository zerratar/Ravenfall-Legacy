using System;
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    internal class WebBasedVillageEndpoint : IVillageEndpoint
    {
        private readonly IApiRequestBuilderProvider request;
        private readonly IRavenNestClient client;
        private readonly ILogger logger;
        public WebBasedVillageEndpoint(
            IRavenNestClient client,
            ILogger logger,
            IApiRequestBuilderProvider request)
        {
            this.client = client;
            this.logger = logger;
            this.request = request;
        }

        public Task<bool> AssignPlayerAsync(int slot, string userId)
        {
            return request.Create()
                    .Identifier(slot.ToString())
                    .Method("assign")
                    .AddParameter(userId)
                    .Build()
                    .SendAsync<bool>(
                        ApiRequestTarget.Village,
                        ApiRequestType.Get);
        }

        public Task<bool> BuildHouseAsync(int slot, int type)
        {
            return request.Create()
                .Identifier(slot.ToString())
                .Method("build")
                .AddParameter(type.ToString())
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Village,
                    ApiRequestType.Get);
        }

        public Task<bool> RemoveHouseAsync(int slot)
        {
            return request.Create()
                .Identifier(slot.ToString())
                .Method("remove")
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Village,
                    ApiRequestType.Get);
        }
    }

    internal class WebBasedMarketplaceEndpoint : IMarketplaceEndpoint
    {
        private readonly IApiRequestBuilderProvider request;
        private readonly IRavenNestClient client;
        private readonly ILogger logger;
        public WebBasedMarketplaceEndpoint(
            IRavenNestClient client,
            ILogger logger,
            IApiRequestBuilderProvider request)
        {
            this.client = client;
            this.logger = logger;
            this.request = request;
        }

        public Task<ItemSellResult> SellItemAsync(string userId, Guid itemId, decimal amount, decimal pricePerItem)
        {
            return request.Create()
                .Identifier(userId)
                .Method("sell")
                .AddParameter(itemId.ToString())
                .AddParameter(((long)amount).ToString())
                .AddParameter(((long)pricePerItem).ToString())
                .Build()
                .SendAsync<ItemSellResult>(
                    ApiRequestTarget.Marketplace,
                    ApiRequestType.Get);
        }

        public Task<ItemBuyResult> BuyItemAsync(string userId, Guid itemId, decimal amount, decimal maxPricePerItem)
        {
            return request.Create()
                .Identifier(userId)
                .Method("buy")
                .AddParameter(itemId.ToString())
                .AddParameter(((long)amount).ToString())
                .AddParameter(((long)maxPricePerItem).ToString())
                .Build()
                .SendAsync<ItemBuyResult>(
                    ApiRequestTarget.Marketplace,
                    ApiRequestType.Get);
        }
    }
}