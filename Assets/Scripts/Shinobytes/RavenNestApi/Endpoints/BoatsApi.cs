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
