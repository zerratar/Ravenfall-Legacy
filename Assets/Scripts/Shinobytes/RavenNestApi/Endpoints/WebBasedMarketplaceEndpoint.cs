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
        public Task<HousePlacement[]> GetPlacementsAsync()
        {
            return request.Create()
                .Method("placements")
                .Build()
                .SendAsync<HousePlacement[]>(
                    ApiRequestTarget.Village,
                    ApiRequestType.Get);
        }

        // ---- Roads: api/village/roads ----

        public Task<RoadType[]> GetRoadTypesAsync()
        {
            return request.Create().Identifier("roads").Method("types").Build()
                .SendAsync<RoadType[]>(ApiRequestTarget.Village, ApiRequestType.Get);
        }

        public Task<RoadInfo[]> GetRoadsAsync()
        {
            return request.Create().Identifier("roads").Build()
                .SendAsync<RoadInfo[]>(ApiRequestTarget.Village, ApiRequestType.Get);
        }

        public Task<RoadResult> BuildRoadAsync(int type, double[] points, double[] widths = null, double startRound = 1, double endRound = 1)
        {
            return request.Create().Identifier("roads").Method("build").Build()
                .SendAsync<RoadResult, RoadRequest>(ApiRequestTarget.Village, ApiRequestType.Post, new RoadRequest { Type = type, Points = points, Widths = widths, StartRound = startRound, EndRound = endRound });
        }

        public Task<RoadResult> ChangeRoadAsync(Guid id, int type, double[] points, double[] widths = null, double startRound = 1, double endRound = 1)
        {
            return request.Create().Identifier("roads").Method(id.ToString()).AddParameter("change").Build()
                .SendAsync<RoadResult, RoadRequest>(ApiRequestTarget.Village, ApiRequestType.Post, new RoadRequest { Type = type, Points = points, Widths = widths, StartRound = startRound, EndRound = endRound });
        }

        public Task<RoadResult> RemoveRoadAsync(Guid id)
        {
            return request.Create().Identifier("roads").Method(id.ToString()).AddParameter("remove").Build()
                .SendAsync<RoadResult>(ApiRequestTarget.Village, ApiRequestType.Post);
        }

        // ---- Decor: api/village/decor (docs/village-decor-design.md) ----

        public Task<DecorPiece[]> GetDecorPiecesAsync()
        {
            return request.Create().Identifier("decor").Method("pieces").Build()
                .SendAsync<DecorPiece[]>(ApiRequestTarget.Village, ApiRequestType.Get);
        }

        public Task<DecorInfo[]> GetDecorAsync()
        {
            return request.Create().Identifier("decor").Build()
                .SendAsync<DecorInfo[]>(ApiRequestTarget.Village, ApiRequestType.Get);
        }

        public Task<DecorResult> PlaceDecorAsync(DecorRequest decor)
        {
            return request.Create().Identifier("decor").Method("place").Build()
                .SendAsync<DecorResult, DecorRequest>(ApiRequestTarget.Village, ApiRequestType.Post, decor);
        }

        public Task<DecorResult> MoveDecorAsync(DecorRequest decor)
        {
            return request.Create().Identifier("decor").Method("move").Build()
                .SendAsync<DecorResult, DecorRequest>(ApiRequestTarget.Village, ApiRequestType.Post, decor);
        }

        public Task<DecorResult> RemoveDecorAsync(DecorRequest decor)
        {
            return request.Create().Identifier("decor").Method("remove").Build()
                .SendAsync<DecorResult, DecorRequest>(ApiRequestTarget.Village, ApiRequestType.Post, decor);
        }

        /// <summary>Unlocks a Special piece for good with the village treasury's coins.</summary>
        public Task<DecorResult> UnlockDecorAsync(DecorRequest decor)
        {
            return request.Create().Identifier("decor").Method("unlock").Build()
                .SendAsync<DecorResult, DecorRequest>(ApiRequestTarget.Village, ApiRequestType.Post, decor);
        }

        /// <summary>A viewer's !decor gift, paid with their loyalty points with this streamer.</summary>
        public Task<DecorGiftResult> GiftDecorAsync(DecorGiftRequest gift)
        {
            return request.Create().Identifier("decor").Method("gift").Build()
                .SendAsync<DecorGiftResult, DecorGiftRequest>(ApiRequestTarget.Village, ApiRequestType.Post, gift);
        }

        public Task<bool> PlaceHouseAsync(int slot, double x, double z, double rotation)
        {
            return request.Create()
                .Identifier(slot.ToString())
                .Method("place")
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Village, ApiRequestType.Post, new HousePlacement
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

    /// <summary>Mirrors RavenNest.Models.VillageRoadType: a kind of road, and what a metre of it costs.</summary>
    public class RoadType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Width { get; set; }
        /// <summary>The town hall tier it unlocks at, 0 to 4 (shown as 1 to 5).</summary>
        public int Tier { get; set; }
        public bool Unlocked { get; set; }
        public double Wood { get; set; }
        public double Ore { get; set; }
        public double Fish { get; set; }
        public double Wheat { get; set; }
        public double Coins { get; set; }
    }

    /// <summary>
    /// Mirrors RavenNest.Models.VillageRoadInfo. Points are x, z pairs; Widths one per point (null: 1
    /// everywhere); the ends 1 round to 0 square.
    /// </summary>
    public class RoadInfo
    {
        public Guid Id { get; set; }
        public int Type { get; set; }
        public double[] Points { get; set; }
        public double[] Widths { get; set; }
        public double StartRound { get; set; } = 1;
        public double EndRound { get; set; } = 1;
    }

    public class RoadRequest
    {
        public int Type { get; set; }
        public double[] Points { get; set; }
        public double[] Widths { get; set; }
        public double? StartRound { get; set; }
        public double? EndRound { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.VillageRoadResult, with the village's resources after it.</summary>
    public class RoadResult
    {
        public bool Ok { get; set; }
        /// <summary>locked, resources, invalid or unknown.</summary>
        public string Error { get; set; }
        public RoadInfo Road { get; set; }
        public double Wood { get; set; }
        public double Ore { get; set; }
        public double Fish { get; set; }
        public double Wheat { get; set; }
        public double Coins { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.VillageDecorPiece: a kind of decor, what it costs and whether the village has it.</summary>
    public class DecorPiece
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }
        public bool Small { get; set; }
        public int Tier { get; set; }
        public int Level { get; set; }
        /// <summary>The seasonal event that unlocks it (christmas, halloween), or null.</summary>
        public string Event { get; set; }
        /// <summary>Treasury coins to unlock it once (the Special pieces), or 0.</summary>
        public double UnlockCoins { get; set; }
        /// <summary>The quest chain that unlocks it where a viewer finishes it ("cat"), or null.</summary>
        public string Quest { get; set; }
        /// <summary>A viewer's price in loyalty points (!decor gift).</summary>
        public long Points { get; set; }
        /// <summary>Viewers' gifts of it waiting to be placed, and the last givers' names, comma separated.</summary>
        public long Gifts { get; set; }
        public string GiftedBy { get; set; }
        public bool Unlocked { get; set; }
        public double Wood { get; set; }
        public double Ore { get; set; }
        public double Fish { get; set; }
        public double Wheat { get; set; }
        public double Coins { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.VillageDecorInfo: a placed piece, or a change to one of the island's own.</summary>
    public class DecorInfo
    {
        public Guid Id { get; set; }
        public string Piece { get; set; }
        public string DefaultKey { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Rotation { get; set; }
        public double Scale { get; set; } = 1;
        public bool Removed { get; set; }
        public bool Gift { get; set; }
    }

    public class DecorRequest
    {
        public string Piece { get; set; }
        public Guid? Id { get; set; }
        public string DefaultKey { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Rotation { get; set; }
        public double Scale { get; set; } = 1;
        /// <summary>Placing one of the viewers' gifts of the piece, free.</summary>
        public bool Gift { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.VillageDecorGiftRequest.</summary>
    public class DecorGiftRequest
    {
        public string Piece { get; set; }
        public string Platform { get; set; }
        public string PlatformId { get; set; }
        public string UserName { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.VillageDecorGiftResult.</summary>
    public class DecorGiftResult
    {
        public bool Ok { get; set; }
        /// <summary>unknown, locked, nouser, points or invalid.</summary>
        public string Error { get; set; }
        public string Piece { get; set; }
        public string PieceName { get; set; }
        public long Points { get; set; }
        public long PointsLeft { get; set; }
        public long Gifts { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.VillageDecorResult, with what removing gave back and the resources after it.</summary>
    public class DecorResult
    {
        public bool Ok { get; set; }
        /// <summary>locked, resources, invalid, unknown, removed, nogift or unlocked.</summary>
        public string Error { get; set; }
        public DecorInfo Decor { get; set; }
        public double RefundWood { get; set; }
        public double RefundOre { get; set; }
        public double RefundFish { get; set; }
        public double RefundWheat { get; set; }
        public double RefundCoins { get; set; }
        public double Wood { get; set; }
        public double Ore { get; set; }
        public double Fish { get; set; }
        public double Wheat { get; set; }
        public double Coins { get; set; }
    }

    /// <summary>
    /// Mirrors RavenNest.Models.VillageHousePlacement. Defined here rather than in the shared
    /// models library, which is a prebuilt DLL in this project.
    /// </summary>
    public class HousePlacement
    {
        public int Slot { get; set; }
        public double X { get; set; }
        public double Z { get; set; }
        public double Rotation { get; set; }
    }

    /// <summary>
    /// Mirrors RavenNest.Models.VillageTownHall, defined here for the same reason as HousePlacement.
    /// Null X and Z mean the town hall stands on the plaza.
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