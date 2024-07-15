using System;
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    public class MarketplaceApi
    {
        private readonly IApiRequestBuilderProvider request;
        private readonly RavenNestClient client;
        private readonly ILogger logger;
        public MarketplaceApi(
            RavenNestClient client,
            ILogger logger,
            IApiRequestBuilderProvider request)
        {
            this.client = client;
            this.logger = logger;
            this.request = request;
        }

        public Task<ItemSellResult> SellItemAsync(Guid characterId, Guid itemId, long amount, long pricePerItem)
        {
            return request.Create()
                .Identifier("v2/" + characterId)
                .Method("sell")
                .AddParameter(itemId.ToString())
                .AddParameter((amount).ToString())
                .AddParameter((pricePerItem).ToString())
                .Build()
                .SendAsync<ItemSellResult>(
                    ApiRequestTarget.Marketplace,
                    ApiRequestType.Get);
        }

        public Task<ItemBuyResult> BuyItemAsync(Guid characterId, Guid itemId, long amount, long maxPricePerItem)
        {
            return request.Create()
                .Identifier("v2/" + characterId)
                .Method("buy")
                .AddParameter(itemId.ToString())
                .AddParameter(amount.ToString())
                .AddParameter(maxPricePerItem.ToString())
                .Build()
                .SendAsync<ItemBuyResult>(
                    ApiRequestTarget.Marketplace,
                    ApiRequestType.Get);
        }

        public Task<ItemValueResult> GetMarketValueAsync(Guid itemId, long count = 1)
        {
            return request.Create()
                .Identifier("v2")
                .Method("value")
                .AddParameter(itemId.ToString())
                .AddParameter(count.ToString())
                .Build()
                .SendAsync<ItemValueResult>(
                    ApiRequestTarget.Marketplace,
                    ApiRequestType.Get);
        }
    }
}