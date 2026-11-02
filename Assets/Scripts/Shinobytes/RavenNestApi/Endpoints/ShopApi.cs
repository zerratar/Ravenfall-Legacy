using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    /// <summary>
    /// The tavern shelves (api/shop): the streamer lists their own items for coins that go to the
    /// village treasury, and viewers buy them one at a time with !redeem. See
    /// docs/streamer-shop-design.md.
    /// </summary>
    public class ShopApi
    {
        private readonly IApiRequestBuilderProvider request;

        public ShopApi(IApiRequestBuilderProvider request)
        {
            this.request = request;
        }

        public Task<StreamerShopInfo> GetAsync()
        {
            return request.Create().Build().SendAsync<StreamerShopInfo>(ApiRequestTarget.Shop, ApiRequestType.Get);
        }

        public Task<StreamerShopSources> GetSourcesAsync()
        {
            return request.Create().Method("sources").Build().SendAsync<StreamerShopSources>(ApiRequestTarget.Shop, ApiRequestType.Get);
        }

        public Task<StreamerShopReport> GetReportAsync()
        {
            return request.Create().Method("report").Build().SendAsync<StreamerShopReport>(ApiRequestTarget.Shop, ApiRequestType.Get);
        }

        public Task<StreamerShopResult> ListAsync(StreamerShopListingRequest listing)
        {
            return request.Create().Method("list").Build()
                .SendAsync<StreamerShopResult, StreamerShopListingRequest>(ApiRequestTarget.Shop, ApiRequestType.Post, listing);
        }

        public Task<StreamerShopResult> EditAsync(Guid listingId, StreamerShopListingRequest changes)
        {
            return request.Create().Identifier(listingId.ToString()).Method("edit").Build()
                .SendAsync<StreamerShopResult, StreamerShopListingRequest>(ApiRequestTarget.Shop, ApiRequestType.Post, changes);
        }

        public Task<StreamerShopResult> RemoveAsync(Guid listingId)
        {
            return request.Create().Identifier(listingId.ToString()).Method("remove").Build()
                .SendAsync<StreamerShopResult>(ApiRequestTarget.Shop, ApiRequestType.Post);
        }

        public Task<StreamerShopBuyResult> BuyAsync(Guid listingId, Guid characterId, long amount = 1)
        {
            return request.Create().Identifier(listingId.ToString()).Method("buy").AddParameter(characterId.ToString()).AddParameter(Math.Max(1, amount).ToString()).Build()
                .SendAsync<StreamerShopBuyResult>(ApiRequestTarget.Shop, ApiRequestType.Get);
        }
    }
}
