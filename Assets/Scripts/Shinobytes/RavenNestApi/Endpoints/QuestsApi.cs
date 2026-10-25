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

        /// <summary>Takes one of today's offers (slot 0 to 2), this week's quest (slot -1) or today's seasonal quest (-2).</summary>
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

        /// <summary>Begins a quest chain ("cat", "fish", "founder", "shipwright", "pumpkin").</summary>
        public Task<QuestActionData> BeginChainAsync(Guid characterId, string key)
        {
            return request.Create().Identifier(characterId.ToString()).Method("chain").AddParameter(key).Build()
                .SendAsync<QuestActionData>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>Hands in what the chain's delivery step asks for, from the character's inventory.</summary>
        public Task<QuestActionData> DeliverAsync(Guid characterId)
        {
            return request.Create().Identifier(characterId.ToString()).Method("deliver").Build()
                .SendAsync<QuestActionData>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>Answers the chain's riddle or choice (from 1).</summary>
        public Task<QuestActionData> AnswerAsync(Guid characterId, int answer)
        {
            return request.Create().Identifier(characterId.ToString()).Method("answer").AddParameter(answer.ToString()).Build()
                .SendAsync<QuestActionData>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>The character's achievements, the titles earned and the one worn.</summary>
        public Task<AchievementListData> GetAchievementsAsync(Guid characterId)
        {
            return request.Create().Identifier(characterId.ToString()).Method("achievements").Build()
                .SendAsync<AchievementListData>(ApiRequestTarget.Quests, ApiRequestType.Get);
        }

        /// <summary>Wears an earned title, or none with "none".</summary>
        public Task<TitleResultData> SetTitleAsync(Guid characterId, string title)
        {
            return request.Create().Identifier(characterId.ToString()).Method("title").AddParameter(title).Build()
                .SendAsync<TitleResultData>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>The titles worn by the characters in this stream, for the name tags.</summary>
        public Task<SessionTitlesData> GetTitlesAsync()
        {
            return request.Create().Identifier("titles").Build()
                .SendAsync<SessionTitlesData>(ApiRequestTarget.Quests, ApiRequestType.Get);
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
        /// <summary>0 daily, 1 weekly, 2 seasonal daily, 3 chain.</summary>
        public int Kind { get; set; }
        public int Slot { get; set; }
        /// <summary>gather, exp, craft, cook, brew, kill, raid, dungeon, or for chain steps deliver, visit or answer.</summary>
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
        /// <summary>A chain's current step (from 0) and how many it has.</summary>
        public int Step { get; set; }
        public int Steps { get; set; }
        /// <summary>A chain step's item to deliver ("Raw Sprat") or island to visit ("Kyo").</summary>
        public string Target { get; set; }
        /// <summary>An answer step's number of choices.</summary>
        public int Options { get; set; }
        public string RewardTitle { get; set; }
        public string RewardDecor { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.QuestChainInfo.</summary>
    public class QuestChainData
    {
        public string Key { get; set; }
        /// <summary>0 can be begun, 1 under way, 2 finished, 5 not now (a seasonal chain outside its event).</summary>
        public int State { get; set; }
        public int Step { get; set; }
        public int Steps { get; set; }
        public string Event { get; set; }
        public string RewardTitle { get; set; }
        public string RewardDecor { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.QuestList.</summary>
    public class QuestListData
    {
        public QuestData[] Active { get; set; }
        public QuestData[] Offers { get; set; }
        public QuestData Weekly { get; set; }
        public int Streak { get; set; }
        public long DailiesCompleted { get; set; }
        public QuestData Seasonal { get; set; }
        public QuestData Chain { get; set; }
        public QuestChainData[] Chains { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.QuestActionResult.</summary>
    public class QuestActionData
    {
        public bool Ok { get; set; }
        /// <summary>unknown, full, taken, done, expired, notinsession, busy, notnow, nothing, wrong or notstep.</summary>
        public string Error { get; set; }
        public QuestData Quest { get; set; }
        public long Delivered { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.QuestCompleted, the QuestCompleted event's data.</summary>
    public class QuestCompletedData
    {
        public Guid CharacterId { get; set; }
        /// <summary>The quest; for a chain step that was not the last, the chain at its next step (State 1).</summary>
        public QuestData Quest { get; set; }
        public int Streak { get; set; }
        public int StepDone { get; set; }
        public string DecorUnlocked { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.AchievementInfo.</summary>
    public class AchievementData
    {
        public string Key { get; set; }
        public long Progress { get; set; }
        /// <summary>0 none, 1 bronze, 2 silver, 3 gold.</summary>
        public int Tier { get; set; }
        public long[] Targets { get; set; }
        public bool Hidden { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.AchievementList.</summary>
    public class AchievementListData
    {
        public AchievementData[] Achievements { get; set; }
        public string[] Titles { get; set; }
        public string Title { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.TitleResult.</summary>
    public class TitleResultData
    {
        public bool Ok { get; set; }
        public string Error { get; set; }
        public string Title { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.SessionTitles.</summary>
    public class SessionTitlesData
    {
        public Guid[] CharacterIds { get; set; }
        public string[] Titles { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.AchievementReached, the AchievementReached event's data.</summary>
    public class AchievementReachedData
    {
        public Guid CharacterId { get; set; }
        public string Key { get; set; }
        public int Tier { get; set; }
        public string Title { get; set; }
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
