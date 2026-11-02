using RavenNest.Models;
using System;
using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    public class VillageApi
    {
        private readonly IApiRequestBuilderProvider request;
        private readonly RavenNestClient client;
        private readonly ILogger logger;
        public VillageApi(
            RavenNestClient client,
            ILogger logger,
            IApiRequestBuilderProvider request)
        {
            this.client = client;
            this.logger = logger;
            this.request = request;
        }

        public Task<bool> AssignPlayerAsync(int slot, Guid characterId)
        {
            return request.Create()
                    .Identifier(slot.ToString())
                    .Method("assign-character")
                    .AddParameter(characterId.ToString())
                    .Build()
                    .SendAsync<bool>(
                        ApiRequestTarget.Village,
                        ApiRequestType.Get);
        }
        public Task<bool> BuildHouseAsync(int slot, int type)
        {
            return request.Create()
                .Identifier(slot.ToString())
                .Method("build")
                .AddParameter(type.ToString())
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Village,
                    ApiRequestType.Get);
        }

        public Task<bool> AssignVillageAsync(int type, Guid[] characterIds)
        {
            return request.Create()
                .Method("assign-village")
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Village, ApiRequestType.Post, new VillageAssignRequest
                {
                    Type = type,
                    CharacterIds = characterIds
                });
        }

        public Task<VillageInfo> GetAsync()
        {
            return request.Create()
               .Build()
               .SendAsync<VillageInfo>(
                   ApiRequestTarget.Village,
                   ApiRequestType.Get);
        }

        /// <summary>
        /// Positions of houses moved in build mode. Null when the server is older than build mode
        /// placement or the call failed; houses missing from the list are at their original plots.
        /// </summary>
        public Task<VillageHousePlacement[]> GetPlacementsAsync()
        {
            return request.Create()
                .Method("placements")
                .Build()
                .SendAsync<VillageHousePlacement[]>(
                    ApiRequestTarget.Village,
                    ApiRequestType.Get);
        }

        // ---- Roads: api/village/roads ----

        public Task<VillageRoadType[]> GetRoadTypesAsync()
        {
            return request.Create().Identifier("roads").Method("types").Build()
                .SendAsync<VillageRoadType[]>(ApiRequestTarget.Village, ApiRequestType.Get);
        }

        public Task<VillageRoadInfo[]> GetRoadsAsync()
        {
            return request.Create().Identifier("roads").Build()
                .SendAsync<VillageRoadInfo[]>(ApiRequestTarget.Village, ApiRequestType.Get);
        }

        public Task<VillageRoadResult> BuildRoadAsync(int type, double[] points, double[] widths = null, double startRound = 1, double endRound = 1)
        {
            return request.Create().Identifier("roads").Method("build").Build()
                .SendAsync<VillageRoadResult, VillageRoadRequest>(ApiRequestTarget.Village, ApiRequestType.Post, new VillageRoadRequest { Type = type, Points = points, Widths = widths, StartRound = startRound, EndRound = endRound });
        }

        public Task<VillageRoadResult> ChangeRoadAsync(Guid id, int type, double[] points, double[] widths = null, double startRound = 1, double endRound = 1)
        {
            return request.Create().Identifier("roads").Method(id.ToString()).AddParameter("change").Build()
                .SendAsync<VillageRoadResult, VillageRoadRequest>(ApiRequestTarget.Village, ApiRequestType.Post, new VillageRoadRequest { Type = type, Points = points, Widths = widths, StartRound = startRound, EndRound = endRound });
        }

        public Task<VillageRoadResult> RemoveRoadAsync(Guid id)
        {
            return request.Create().Identifier("roads").Method(id.ToString()).AddParameter("remove").Build()
                .SendAsync<VillageRoadResult>(ApiRequestTarget.Village, ApiRequestType.Post);
        }

        // ---- Decor: api/village/decor (docs/village-decor-design.md) ----

        public Task<VillageDecorPiece[]> GetDecorPiecesAsync()
        {
            return request.Create().Identifier("decor").Method("pieces").Build()
                .SendAsync<VillageDecorPiece[]>(ApiRequestTarget.Village, ApiRequestType.Get);
        }

        public Task<VillageDecorInfo[]> GetDecorAsync()
        {
            return request.Create().Identifier("decor").Build()
                .SendAsync<VillageDecorInfo[]>(ApiRequestTarget.Village, ApiRequestType.Get);
        }

        public Task<VillageDecorResult> PlaceDecorAsync(VillageDecorRequest decor)
        {
            return request.Create().Identifier("decor").Method("place").Build()
                .SendAsync<VillageDecorResult, VillageDecorRequest>(ApiRequestTarget.Village, ApiRequestType.Post, decor);
        }

        public Task<VillageDecorResult> MoveDecorAsync(VillageDecorRequest decor)
        {
            return request.Create().Identifier("decor").Method("move").Build()
                .SendAsync<VillageDecorResult, VillageDecorRequest>(ApiRequestTarget.Village, ApiRequestType.Post, decor);
        }

        public Task<VillageDecorResult> RemoveDecorAsync(VillageDecorRequest decor)
        {
            return request.Create().Identifier("decor").Method("remove").Build()
                .SendAsync<VillageDecorResult, VillageDecorRequest>(ApiRequestTarget.Village, ApiRequestType.Post, decor);
        }

        /// <summary>Unlocks a Special piece for good with the village treasury's coins.</summary>
        public Task<VillageDecorResult> UnlockDecorAsync(VillageDecorRequest decor)
        {
            return request.Create().Identifier("decor").Method("unlock").Build()
                .SendAsync<VillageDecorResult, VillageDecorRequest>(ApiRequestTarget.Village, ApiRequestType.Post, decor);
        }

        /// <summary>A viewer's !decor gift, paid with their loyalty points with this streamer.</summary>
        public Task<VillageDecorGiftResult> GiftDecorAsync(VillageDecorGiftRequest gift)
        {
            return request.Create().Identifier("decor").Method("gift").Build()
                .SendAsync<VillageDecorGiftResult, VillageDecorGiftRequest>(ApiRequestTarget.Village, ApiRequestType.Post, gift);
        }

        public Task<bool> PlaceHouseAsync(int slot, double x, double z, double rotation)
        {
            return request.Create()
                .Identifier(slot.ToString())
                .Method("place")
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Village, ApiRequestType.Post, new VillageHousePlacement
                {
                    Slot = slot,
                    X = x,
                    Z = z,
                    Rotation = rotation
                });
        }

        public Task<bool> ResetPlacementAsync(int slot)
        {
            return request.Create()
                .Identifier(slot.ToString())
                .Method("reset-placement")
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Village,
                    ApiRequestType.Get);
        }

        /// <summary>
        /// Where the streamer moved the town hall. Null when the server is older than the movable town
        /// hall or the call failed; null X and Z mean the plaza.
        /// </summary>
        public Task<TownHallPlacement> GetTownHallAsync()
        {
            return request.Create()
                .Method("townhall")
                .Build()
                .SendAsync<TownHallPlacement>(
                    ApiRequestTarget.Village,
                    ApiRequestType.Get);
        }

        public Task<bool> PlaceTownHallAsync(double x, double z, double rotation)
        {
            return request.Create()
                .Identifier("townhall")
                .Method("place")
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Village, ApiRequestType.Post, new TownHallPlacement
                {
                    X = x,
                    Z = z,
                    Rotation = rotation
                });
        }

        /// <summary>Buys the next town hall tier. The town hall afterwards, or null when it was not possible.</summary>
        public Task<TownHallPlacement> UpgradeTownHallAsync()
        {
            return request.Create()
                .Identifier("townhall")
                .Method("upgrade")
                .Build()
                .SendAsync<TownHallPlacement>(
                    ApiRequestTarget.Village,
                    ApiRequestType.Get);
        }

        public Task<bool> ResetTownHallPlacementAsync()
        {
            return request.Create()
                .Identifier("townhall")
                .Method("reset-placement")
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Village,
                    ApiRequestType.Get);
        }

        public Task<bool> RemoveHouseAsync(int slot)
        {
            return request.Create()
                .Identifier(slot.ToString())
                .Method("remove")
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Village,
                    ApiRequestType.Get);
        }
    }

    /// <summary>
    /// Mirrors RavenNest.Models.VillageTownHall, kept here because Tier and MaxTier are nullable to tell a
    /// server without upgrades apart (the server's are plain ints). Null X and Z mean the town hall stands on the plaza.
    /// </summary>
    public class TownHallPlacement
    {
        public double? X { get; set; }
        public double? Z { get; set; }
        public double Rotation { get; set; }

        /// <summary>The tier the village has bought, 0 to MaxTier. Null from a server without upgrades.</summary>
        public int? Tier { get; set; }
        public int? MaxTier { get; set; }

        /// <summary>What the next tier needs. All zero at the last tier.</summary>
        public int NextLevel { get; set; }
        public int NextWood { get; set; }
        public int NextOre { get; set; }
        public int NextFish { get; set; }
        public int NextWheat { get; set; }
    }

    public class VillageAssignRequest
    {
        public int Type { get; set; }
        public Guid[] CharacterIds { get; set; }
    }
}