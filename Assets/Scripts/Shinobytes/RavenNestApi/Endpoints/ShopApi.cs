using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

    // Copies of the server's models (RavenNest.Models/StreamerShop.cs), kept here like the road models
    // rather than in the old RavenNest.Models.dll.

    public enum StreamerShopSourceKind { Stash = 0, Character = 1 }

    public enum StreamerShopAccess { Everyone = 0, Clan = 1, ClanRoles = 2 }

    public class StreamerShopInfo
    {
        public int Shelves { get; set; }
        public long VillageCoins { get; set; }
        public long PriceCap { get; set; }
        public int MinHours { get; set; }
        public int MaxHours { get; set; }
        public List<StreamerShopListingInfo> Listings { get; set; } = new List<StreamerShopListingInfo>();
    }

    public class StreamerShopListingInfo
    {
        public Guid Id { get; set; }
        public int Shelf { get; set; }
        public Guid ItemId { get; set; }
        public long Amount { get; set; }
        public long PricePerItem { get; set; }
        public string Name { get; set; }
        public string Enchantment { get; set; }
        public string Tag { get; set; }
        public Guid? TransmogrificationId { get; set; }
        public int Flags { get; set; }
        public StreamerShopSourceKind SourceKind { get; set; }
        public Guid? SourceCharacterId { get; set; }
        public StreamerShopAccess Access { get; set; }
        public List<Guid> AllowedClanRoleIds { get; set; } = new List<Guid>();
        public int MaxPerViewer { get; set; }
        public long Sold { get; set; }
        public long CoinsEarned { get; set; }
        public DateTime Created { get; set; }
        public DateTime Expires { get; set; }
    }

    public class StreamerShopListingRequest
    {
        public int Shelf { get; set; }
        public StreamerShopSourceKind SourceKind { get; set; }
        public Guid? SourceCharacterId { get; set; }
        public Guid ItemId { get; set; }
        public string Tag { get; set; }
        public string Enchantment { get; set; }
        public Guid? TransmogrificationId { get; set; }
        public string Name { get; set; }
        public int Flags { get; set; }
        public long Amount { get; set; }
        public long PricePerItem { get; set; }
        public int DurationHours { get; set; }
        public StreamerShopAccess Access { get; set; }
        public List<Guid> AllowedClanRoleIds { get; set; } = new List<Guid>();
        public int MaxPerViewer { get; set; }
    }

    public class StreamerShopResult
    {
        public bool Ok { get; set; }
        public string Error { get; set; }
        public StreamerShopListingInfo Listing { get; set; }
        public StreamerShopInfo Shop { get; set; }
    }

    public class StreamerShopSources
    {
        public List<StreamerShopSource> Sources { get; set; } = new List<StreamerShopSource>();
        public List<StreamerShopClanRole> ClanRoles { get; set; } = new List<StreamerShopClanRole>();
        public bool HasClan { get; set; }
    }

    public class StreamerShopSource
    {
        public StreamerShopSourceKind Kind { get; set; }
        public Guid? CharacterId { get; set; }
        public string Name { get; set; }
        public List<StreamerShopSellableItem> Items { get; set; } = new List<StreamerShopSellableItem>();
    }

    public class StreamerShopSellableItem
    {
        public Guid ItemId { get; set; }
        public string ItemName { get; set; }
        public long Amount { get; set; }
        public string Tag { get; set; }
        public string Enchantment { get; set; }
        public Guid? TransmogrificationId { get; set; }
        public string Name { get; set; }
        public int Flags { get; set; }
        public long VendorPrice { get; set; }
        public bool Sellable { get; set; }
        public string Reason { get; set; }
    }

    public class StreamerShopClanRole
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
    }

    public enum StreamerShopBuyResultCode
    {
        Success = 0,
        NotFound = 1,
        SoldOut = 2,
        NotAllowed = 3,
        LimitReached = 4,
        InsufficientCoins = 5,
        OwnListing = 6,
        NotInSession = 7,
        Error = 8,
        NotEnoughLeft = 9
    }

    public class StreamerShopBuyResult
    {
        public StreamerShopBuyResultCode Code { get; set; }
        public string Error { get; set; }
        public Guid ListingId { get; set; }
        public int Shelf { get; set; }
        public Guid ItemId { get; set; }
        public string ItemName { get; set; }
        public Guid InventoryItemId { get; set; }
        public long Amount { get; set; }
        public long Price { get; set; }
        public long MaxAmount { get; set; }
        public long CoinsLeft { get; set; }
        public long LoyaltyPoints { get; set; }
        public long AmountLeft { get; set; }
    }

    public class StreamerShopReport
    {
        public long TotalCoins { get; set; }
        public long TotalItems { get; set; }
        public long CoinsLast24Hours { get; set; }
        public List<StreamerShopSale> Sales { get; set; } = new List<StreamerShopSale>();
    }

    public class StreamerShopSale
    {
        public DateTime Time { get; set; }
        public string BuyerName { get; set; }
        public Guid ItemId { get; set; }
        public string ItemName { get; set; }
        public long Amount { get; set; }
        public long Price { get; set; }
        public long LoyaltyPoints { get; set; }
    }
}
