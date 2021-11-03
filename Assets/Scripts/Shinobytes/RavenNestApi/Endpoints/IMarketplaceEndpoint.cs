using System;
using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    public interface IMarketplaceEndpoint
    {
        Task<RavenNest.Models.ItemSellResult> SellItemAsync(string userId, Guid itemId, long amount, long pricePerItem);
        Task<RavenNest.Models.ItemBuyResult> BuyItemAsync(string userId, Guid itemId, long amount, long maxPricePerItem);
    }
}