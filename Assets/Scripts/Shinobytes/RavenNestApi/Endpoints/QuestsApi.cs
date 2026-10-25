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

        /// <summary>The stream's quest: the running one and today's three offers (api/quests/stream).</summary>
        public Task<StreamQuestStateData> GetStreamAsync()
        {
            return request.Create().Identifier("stream").Build()
                .SendAsync<StreamQuestStateData>(ApiRequestTarget.Quests, ApiRequestType.Get);
        }

        /// <summary>Starts one of today's stream quest offers (slot 0 to 2).</summary>
        public Task<StreamQuestResultData> StartStreamAsync(int slot)
        {
            return request.Create().Identifier("stream").Method("start").AddParameter(slot.ToString()).Build()
                .SendAsync<StreamQuestResultData>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        public Task<StreamQuestResultData> StopStreamAsync()
        {
            return request.Create().Identifier("stream").Method("stop").Build()
                .SendAsync<StreamQuestResultData>(ApiRequestTarget.Quests, ApiRequestType.Post);
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

    /// <summary>Mirrors RavenNest.Models.StreamQuestInfo: a goal the whole stream works on together.</summary>
    public class StreamQuestData
    {
        public Guid Id { get; set; }
        public string Key { get; set; }
        public int Slot { get; set; }
        /// <summary>gather, kill, make, levels, raid or dungeon.</summary>
        public string Objective { get; set; }
        public int Skill { get; set; }
        public long Amount { get; set; }
        public long Progress { get; set; }
        /// <summary>0 offered, 1 running, 2 done, 3 out of time, 4 stopped.</summary>
        public int State { get; set; }
        public int Minutes { get; set; }
        public DateTime Ends { get; set; }
        public long RewardVillageCoins { get; set; }
        /// <summary>1 wood, 2 ore, 3 fish, 4 wheat, or 0.</summary>
        public int RewardResource { get; set; }
        public long RewardResourceAmount { get; set; }
        public long RewardHelperCoins { get; set; }
        public int Helpers { get; set; }
        public StreamQuestHelperData[] TopHelpers { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.StreamQuestHelper.</summary>
    public class StreamQuestHelperData
    {
        public Guid CharacterId { get; set; }
        public string Name { get; set; }
        public long Amount { get; set; }
        public long Coins { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.StreamQuestState.</summary>
    public class StreamQuestStateData
    {
        public StreamQuestData Active { get; set; }
        public StreamQuestData[] Offers { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.StreamQuestResult.</summary>
    public class StreamQuestResultData
    {
        public bool Ok { get; set; }
        /// <summary>running, unknown or none.</summary>
        public string Error { get; set; }
        public StreamQuestData Quest { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.StreamQuestEnded, the StreamQuestEnded event's data.</summary>
    public class StreamQuestEndedData
    {
        public StreamQuestData Quest { get; set; }
        public StreamQuestHelperData[] Helpers { get; set; }
    }
}
