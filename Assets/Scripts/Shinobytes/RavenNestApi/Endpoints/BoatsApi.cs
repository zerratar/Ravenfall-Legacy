using System;
using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    /// <summary>
    /// Own boats on the server (api/boats): what the shipwright on Away builds, and !boat build. See docs/own-boats.md,
    /// phase 4.
    /// </summary>
    public class BoatsApi
    {
        private readonly IApiRequestBuilderProvider request;

        public BoatsApi(IApiRequestBuilderProvider request)
        {
            this.request = request;
        }

        public Task<BoatBlueprintData[]> GetBlueprintsAsync()
        {
            return request.Create().Identifier("blueprints").Build()
                .SendAsync<BoatBlueprintData[]>(ApiRequestTarget.Boats, ApiRequestType.Get);
        }

        public Task<BoatBuildResultData> BuildAsync(Guid characterId, string boat)
        {
            return request.Create().Identifier(characterId.ToString()).Method("build").Build()
                .SendAsync<BoatBuildResultData, BoatBuildRequestData>(ApiRequestTarget.Boats, ApiRequestType.Post, new BoatBuildRequestData { Boat = boat });
        }

        /// <summary>!boat look: the equipped boat's look and what the player owns of each part ("Customizing boats").</summary>
        public Task<BoatLookResultData> GetLookAsync(Guid characterId)
        {
            return request.Create().Identifier(characterId.ToString()).Method("look").Build()
                .SendAsync<BoatLookResultData>(ApiRequestTarget.Boats, ApiRequestType.Get);
        }

        /// <summary>!boat sails 7: one part of the equipped boat's look, to one the player owns.</summary>
        public Task<BoatLookResultData> SetLookAsync(Guid characterId, string slot, int variant)
        {
            return request.Create().Identifier(characterId.ToString()).Method("look").AddParameter(slot).AddParameter(variant.ToString()).Build()
                .SendAsync<BoatLookResultData>(ApiRequestTarget.Boats, ApiRequestType.Post);
        }

        /// <summary>A guest climbed aboard the owner's boat ("Guests aboard"): the owner gets a cut of their fish at sea.</summary>
        public Task<bool> GuestAboardAsync(Guid ownerId, Guid guestId)
        {
            return request.Create().Identifier(ownerId.ToString()).Method("guest-aboard").AddParameter(guestId.ToString()).Build()
                .SendAsync<bool>(ApiRequestTarget.Boats, ApiRequestType.Post);
        }

        /// <summary>A guest stepped off; a whole trip (they sailed a fair way) counts towards the owner's Ferryman titles.</summary>
        public Task<bool> GuestAshoreAsync(Guid ownerId, Guid guestId, bool trip)
        {
            return request.Create().Identifier(ownerId.ToString()).Method("guest-ashore").AddParameter(guestId.ToString()).AddParameter(trip ? "trip" : "none").Build()
                .SendAsync<bool>(ApiRequestTarget.Boats, ApiRequestType.Post);
        }
    }

    public class BoatLookSlotData
    {
        public string Slot { get; set; }
        public int Count { get; set; }
        public bool CanBeNone { get; set; }
        public int Current { get; set; }
        public int[] Owned { get; set; }
    }

    public class BoatLookResultData
    {
        public bool Ok { get; set; }
        /// <summary>Why not: notinsession, noboat, slot, range, locked.</summary>
        public string Error { get; set; }
        public string Boat { get; set; }
        public string Look { get; set; }
        public Guid InventoryItemId { get; set; }
        public BoatLookSlotData[] Slots { get; set; }
    }

    public class BoatBuildRequestData
    {
        public string Boat { get; set; }
    }

    public class BoatPartData
    {
        public string Item { get; set; }
        public long Amount { get; set; }
        public long Have { get; set; }
    }

    public class BoatBlueprintData
    {
        public string Boat { get; set; }
        public int Sailing { get; set; }
        public int Fishing { get; set; }
        public int Crafting { get; set; }
        public long Fee { get; set; }
        public BoatPartData[] Parts { get; set; }
    }

    public class BoatBuildResultData
    {
        public bool Ok { get; set; }
        /// <summary>Why not: notinsession, unknown, island, level, parts, coins, failed.</summary>
        public string Error { get; set; }
        public string Boat { get; set; }
        public string Island { get; set; }
        public string Skill { get; set; }
        public int Level { get; set; }
        public long Fee { get; set; }
        public long Coins { get; set; }
        public BoatPartData[] Missing { get; set; }
        public BoatBlueprintData[] Blueprints { get; set; }
    }
}
