using System;
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    public interface IItemEndpoint
    {
        Task<ItemCollection> GetAsync();

        Task<RedeemableItemCollection> GetRedeemablesAsync();

        Task<bool> AddItemAsync(Item item);

        Task<bool> UpdateItemAsync(Item item);

        Task<bool> RemoveItemAsync(Guid item);
    }
}