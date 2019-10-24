using System;
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    internal class WebBasedItemsEndpoint : IItemEndpoint
    {
        private readonly IRavenNestClient client;
        private readonly ILogger logger;
        private readonly IApiRequestBuilderProvider request;

        public WebBasedItemsEndpoint(IRavenNestClient client, ILogger logger, IApiRequestBuilderProvider request)
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
    }
}