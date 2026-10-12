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