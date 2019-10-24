using System;
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
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