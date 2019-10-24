using System;
using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    public interface IMarketplaceEndpoint
    {
        Task<RavenNest.Models.ItemSellResult> SellItemAsync(string userId, Guid itemId, decimal amount, decimal pricePerItem);
        Task<RavenNest.Models.ItemBuyResult> BuyItemAsync(string userId, Guid itemId, decimal amount, decimal maxPricePerItem);
    }
}