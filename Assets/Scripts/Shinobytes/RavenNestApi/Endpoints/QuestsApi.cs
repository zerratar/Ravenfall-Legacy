using System;
using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    /// <summary>
    /// A character's quests (api/quests): today's three offers, this week's quest, the quests taken and the daily
    /// streak. Progress is counted on the server; the game asks, takes and gives up. See docs/quests-design.md.
    /// </summary>
    public class QuestsApi
    {
        private readonly IApiRequestBuilderProvider request;

        public QuestsApi(IApiRequestBuilderProvider request)
        {
            this.request = request;
        }

        public Task<QuestListData> GetAsync(Guid characterId)
        {
            return request.Create().Identifier(characterId.ToString()).Build()
                .SendAsync<QuestListData>(ApiRequestTarget.Quests, ApiRequestType.Get);
        }

        /// <summary>Takes one of today's offers (slot 0 to 2), or this week's quest (slot -1).</summary>
        public Task<QuestActionData> AcceptAsync(Guid characterId, int slot)
        {
            return request.Create().Identifier(characterId.ToString()).Method("accept").AddParameter(slot.ToString()).Build()
                .SendAsync<QuestActionData>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        public Task<QuestActionData> AbandonAsync(Guid characterId, Guid questId)
        {
            return request.Create().Identifier(characterId.ToString()).Method("abandon").AddParameter(questId).Build()
                .SendAsync<QuestActionData>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }
    }

    /// <summary>Mirrors RavenNest.Models.QuestInfo: a quest, worded by the game from its key and numbers.</summary>
    public class QuestData
    {
        public Guid Id { get; set; }
        public string Key { get; set; }
        /// <summary>0 daily, 1 weekly.</summary>
        public int Kind { get; set; }
        public int Slot { get; set; }
        /// <summary>gather, exp, craft, cook, brew, kill, raid or dungeon.</summary>
        public string Objective { get; set; }
        /// <summary>The skill's index, or -1.</summary>
        public int Skill { get; set; }
        public long Amount { get; set; }
        public long Progress { get; set; }
        /// <summary>0 offered, 1 taken, 2 finished, 3 expired, 4 given up.</summary>
        public int State { get; set; }
        public long RewardCoins { get; set; }
        public double RewardExp { get; set; }
        public int RewardSkill { get; set; }
        public DateTime Expires { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.QuestList.</summary>
    public class QuestListData
    {
        public QuestData[] Active { get; set; }
        public QuestData[] Offers { get; set; }
        public QuestData Weekly { get; set; }
        public int Streak { get; set; }
        public long DailiesCompleted { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.QuestActionResult.</summary>
    public class QuestActionData
    {
        public bool Ok { get; set; }
        /// <summary>unknown, full, taken, done, expired or notinsession.</summary>
        public string Error { get; set; }
        public QuestData Quest { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.QuestCompleted, the QuestCompleted event's data.</summary>
    public class QuestCompletedData
    {
        public Guid CharacterId { get; set; }
        public QuestData Quest { get; set; }
        public int Streak { get; set; }
    }
}
