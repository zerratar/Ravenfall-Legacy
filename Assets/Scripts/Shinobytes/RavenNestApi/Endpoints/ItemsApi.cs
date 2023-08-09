using System;
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    public class ItemsApi
    {
        private readonly RavenNestClient client;
        private readonly ILogger logger;
        private readonly IApiRequestBuilderProvider request;

        public ItemsApi(RavenNestClient client, ILogger logger, IApiRequestBuilderProvider request)
        {
            this.client = client;
            this.logger = logger;
            this.request = request;
        }

        public Task<ItemCollection> GetAsync()
        {
            return request.Create()
                .Build()
                .SendAsync<ItemCollection>(ApiRequestTarget.Items, ApiRequestType.Get);
        }

        public Task<RedeemableItemCollection> GetRedeemablesAsync()
        {
            return request.Create()
                .Method("redeemable")
                .Build()
                .SendAsync<RedeemableItemCollection>(ApiRequestTarget.Items, ApiRequestType.Get);
        }

        public Task<bool> AddItemAsync(Item item)
        {
            return request.Create().Build()
                .SendAsync<bool, Item>(ApiRequestTarget.Items, ApiRequestType.Post, item);
        }

        public Task<bool> UpdateItemAsync(Item item)
        {
            return request.Create().Build()
                .SendAsync<bool, Item>(ApiRequestTarget.Items, ApiRequestType.Update, item);
        }

        public Task<bool> RemoveItemAsync(Guid item)
        {
            return request.Create()
                .AddParameter(item.ToString())
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Items, ApiRequestType.Remove);
        }

        internal Task<ItemCollection> GetDeltaAsync(DateTime lastModified)
        {

            return request.Create()
                .Method("delta")
                .AddParameter(lastModified.ToString())
                .Build()
                .SendAsync<ItemCollection>(ApiRequestTarget.Items, ApiRequestType.Get);
        }
    }
}