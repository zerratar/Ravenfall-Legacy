using System;
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    /// <summary>
    /// The Mangrove island on the server (api/mangrove, docs/banana-island.md): the chests for a dug up treasure and a
    /// held pirates' raid, the regatta's prizes, and the bananas viewers feed the monkeys.
    /// </summary>
    public class MangroveApi
    {
        public const string Treasure = "treasure";
        public const string Pirates = "pirates";

        private readonly IApiRequestBuilderProvider request;

        public MangroveApi(IApiRequestBuilderProvider request)
        {
            this.request = request;
        }

        /// <summary>A chest for each character ("treasure": they dug; "pirates": they held the raid, the best first).</summary>
        public Task<MangroveRewardData[]> ChestAsync(string kind, Guid[] characterIds)
        {
            return request.Create().Identifier(kind).Method("chest").AddParameter("values", characterIds).Build()
                .SendAsync<MangroveRewardData[]>(ApiRequestTarget.Mangrove, ApiRequestType.Post);
        }

        /// <summary>The regatta's finishers, first to last; the coins each was paid.</summary>
        public Task<MangroveRewardData[]> RegattaAsync(Guid[] finishers)
        {
            return request.Create().Identifier("regatta").AddParameter("values", finishers).Build()
                .SendAsync<MangroveRewardData[]>(ApiRequestTarget.Mangrove, ApiRequestType.Post);
        }

        /// <summary>!banana feed: how many of the character's bananas ("banana") or Golden Bananas ("golden") the monkeys got.</summary>
        public Task<long> FeedAsync(Guid characterId, string kind, long amount)
        {
            return request.Create().Identifier(characterId.ToString()).Method("feed").AddParameter(kind).AddParameter(amount.ToString()).Build()
                .SendAsync<long>(ApiRequestTarget.Mangrove, ApiRequestType.Post);
        }
    }

    public class MangroveRewardData
    {
        public Guid CharacterId { get; set; }
        public long Coins { get; set; }
        public EventItemReward[] Items { get; set; }
        /// <summary>Boat looks earned with it ("sails_9", "flag_12"; docs/own-boats.md, "Customizing boats").</summary>
        public string[] Looks { get; set; }
    }
}
