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

        /// <summary>The stream's streamer quests and what the streamer can set aside (api/quests/streamer).</summary>
        public Task<StreamerQuestListData> GetStreamerAsync()
        {
            return request.Create().Identifier("streamer").Build()
                .SendAsync<StreamerQuestListData>(ApiRequestTarget.Quests, ApiRequestType.Get);
        }

        /// <summary>The running streamer quests with one character's progress on them (!quest).</summary>
        public Task<StreamerQuestData[]> GetStreamerForAsync(Guid characterId)
        {
            return request.Create().Identifier("streamer").Method(characterId.ToString()).Build()
                .SendAsync<StreamerQuestData[]>(ApiRequestTarget.Quests, ApiRequestType.Get);
        }

        /// <summary>Posts a streamer quest; the server sets its reward aside.</summary>
        public Task<StreamerQuestResultData> PostStreamerAsync(StreamerQuestRequestData quest)
        {
            return request.Create().Identifier("streamer").Method("post").Build()
                .SendAsync<StreamerQuestResultData, StreamerQuestRequestData>(ApiRequestTarget.Quests, ApiRequestType.Post, quest);
        }

        public Task<StreamerQuestResultData> StopStreamerAsync(Guid questId)
        {
            return request.Create().Identifier("streamer").Method("stop").AddParameter(questId.ToString()).Build()
                .SendAsync<StreamerQuestResultData>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>The streamer's story lines and what they have unlocked (api/quests/village).</summary>
        public Task<VillageLogData> GetVillageAsync()
        {
            return request.Create().Identifier("village").Build()
                .SendAsync<VillageLogData>(ApiRequestTarget.Quests, ApiRequestType.Get);
        }

        public Task<VillageStoryResultData> BeginStoryAsync(string key) => StoryAction("begin", key);

        /// <summary>The streamer talks to the story's NPC: a talk step is done.</summary>
        public Task<VillageStoryResultData> TalkStoryAsync(string key) => StoryAction("talk", key);

        public Task<VillageStoryResultData> AbandonStoryAsync(string key) => StoryAction("abandon", key);

        /// <summary>The streamer makes the story's choice (from 1).</summary>
        public Task<VillageStoryResultData> ChooseStoryAsync(string key, int option)
        {
            return request.Create().Identifier("village").Method("choose").AddParameter(key).AddParameter(option.ToString()).Build()
                .SendAsync<VillageStoryResultData>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>A viewer brings the story's delivery item from their inventory (!quest give).</summary>
        public Task<VillageStoryResultData> GiveStoryAsync(Guid characterId)
        {
            return request.Create().Identifier("village").Method("give").AddParameter(characterId.ToString()).Build()
                .SendAsync<VillageStoryResultData>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>The streamer's character finished the Ravebridge island quests: the title "of Ravebridge".</summary>
        public Task<VillageStoryResultData> RavebridgeDoneAsync()
        {
            return request.Create().Identifier("village").Method("ravebridge-done").Build()
                .SendAsync<VillageStoryResultData>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>A hero mode wave run ended: the streamer's best, the titles of waves 10, 25 and 50, the best's rank.</summary>
        public Task<HeroRunResultData> HeroRunAsync(int waves, int kills, int seconds)
        {
            return request.Create().Identifier("hero").Method("run").AddParameter(waves.ToString()).AddParameter(kills.ToString()).AddParameter(seconds.ToString()).Build()
                .SendAsync<HeroRunResultData>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>Every streamer's best hero mode run, the best first.</summary>
        public Task<HeroLeaderboardData> HeroLeaderboardAsync()
        {
            return request.Create().Identifier("hero").Method("leaderboard").Build()
                .SendAsync<HeroLeaderboardData>(ApiRequestTarget.Quests, ApiRequestType.Get);
        }

        private Task<VillageStoryResultData> StoryAction(string action, string key)
        {
            return request.Create().Identifier("village").Method(action).AddParameter(key).Build()
                .SendAsync<VillageStoryResultData>(ApiRequestTarget.Quests, ApiRequestType.Post);
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

    /// <summary>Mirrors RavenNest.Models.StreamerQuestInfo: a quest the streamer wrote, a bounty or a stream goal.</summary>
    public class StreamerQuestData
    {
        public Guid Id { get; set; }
        /// <summary>1 bounty, 2 stream goal.</summary>
        public int Kind { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        /// <summary>gather, kill, craft, cook, brew, levels, raid or dungeon.</summary>
        public string Objective { get; set; }
        public int Skill { get; set; }
        public long Amount { get; set; }
        public long Progress { get; set; }
        public int Winners { get; set; }
        public int Claimed { get; set; }
        /// <summary>1 running, 2 done, 3 out of time, 4 stopped, 5 every reward claimed.</summary>
        public int State { get; set; }
        public DateTime Ends { get; set; }
        public int Minutes { get; set; }
        public long RewardCoins { get; set; }
        public Guid? RewardItemId { get; set; }
        public string RewardItemName { get; set; }
        public long RewardItemAmount { get; set; }
        public long RewardLoyalty { get; set; }
        public int Helpers { get; set; }
        public StreamQuestHelperData[] TopHelpers { get; set; }
        public long YourProgress { get; set; }
        public bool YouClaimed { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.StreamerQuestRequest: what the streamer posts.</summary>
    public class StreamerQuestRequestData
    {
        public int Kind { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Objective { get; set; }
        public int Skill { get; set; }
        public long Amount { get; set; }
        public int Minutes { get; set; }
        public int Winners { get; set; }
        public long RewardCoins { get; set; }
        /// <summary>1 the village treasury, 2 the streamer's character.</summary>
        public int CoinSource { get; set; }
        public Guid? CoinCharacterId { get; set; }
        public Guid? ItemId { get; set; }
        public string ItemTag { get; set; }
        public string ItemEnchantment { get; set; }
        public Guid? ItemTransmogrificationId { get; set; }
        public string ItemName { get; set; }
        public int ItemFlags { get; set; }
        public long ItemAmount { get; set; }
        public StreamerShopSourceKind ItemSourceKind { get; set; }
        public Guid? ItemSourceCharacterId { get; set; }
        public long RewardLoyalty { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.StreamerQuestList.</summary>
    public class StreamerQuestListData
    {
        public StreamerQuestData[] Quests { get; set; }
        public int MaxBounties { get; set; }
        public long LoyaltyBudgetLeft { get; set; }
        public long LoyaltyBudgetWeekly { get; set; }
        public long TreasuryCoins { get; set; }
        public StreamerQuestCoinSourceData[] Characters { get; set; }
    }

    public class StreamerQuestCoinSourceData
    {
        public Guid CharacterId { get; set; }
        public string Name { get; set; }
        public long Coins { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.StreamerQuestResult.</summary>
    public class StreamerQuestResultData
    {
        public bool Ok { get; set; }
        public string Error { get; set; }
        public StreamerQuestData Quest { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.StreamerQuestClaimed, the StreamerQuestClaimed event's data.</summary>
    public class StreamerQuestClaimedData
    {
        public StreamerQuestData Quest { get; set; }
        public Guid CharacterId { get; set; }
        public long Coins { get; set; }
        public long Items { get; set; }
        public long Loyalty { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.StreamerQuestEnded, the StreamerQuestEnded event's data.</summary>
    public class StreamerQuestEndedData
    {
        public StreamerQuestData Quest { get; set; }
        public StreamerQuestRewardData[] Rewards { get; set; }
    }

    public class StreamerQuestRewardData
    {
        public Guid CharacterId { get; set; }
        public string Name { get; set; }
        public long Amount { get; set; }
        public long Coins { get; set; }
        public long Items { get; set; }
        public long Loyalty { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.VillageStoryInfo: one of the streamer's story lines at its current step.</summary>
    public class VillageStoryData
    {
        public string Key { get; set; }
        /// <summary>0 not begun, 1 under way, 2 finished, 4 given up.</summary>
        public int State { get; set; }
        public int Step { get; set; }
        public int Steps { get; set; }
        /// <summary>talk, deliver, gather, kill, raid, dungeon, find, viewers or choice.</summary>
        public string Kind { get; set; }
        public string Target { get; set; }
        public int Skill { get; set; }
        public long Amount { get; set; }
        public long Progress { get; set; }
        public int Options { get; set; }
        /// <summary>"6=2": the choice at step 6 was option 2.</summary>
        public string Choices { get; set; }
        public StreamQuestHelperData[] Helpers { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.VillageLog.</summary>
    public class VillageLogData
    {
        public VillageStoryData[] Stories { get; set; }
        public string[] Unlocks { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.HeroRunRecord.</summary>
    public class HeroRunRecordData
    {
        public string Streamer { get; set; }
        public int Waves { get; set; }
        public int Kills { get; set; }
        public int Seconds { get; set; }
        public DateTime Reached { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.HeroRunResult.</summary>
    public class HeroRunResultData
    {
        public bool Ok { get; set; }
        public string Error { get; set; }
        public HeroRunRecordData Best { get; set; }
        public bool NewBest { get; set; }
        public string[] NewTitles { get; set; }
        public int Rank { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.HeroLeaderboard.</summary>
    public class HeroLeaderboardData
    {
        public HeroRunRecordData[] Entries { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.VillageStoryResult.</summary>
    public class VillageStoryResultData
    {
        public bool Ok { get; set; }
        /// <summary>busy, taken, done, unknown, notstep, nothing, none, notinsession.</summary>
        public string Error { get; set; }
        public VillageStoryData Story { get; set; }
        public long Given { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.VillageStoryProgress, the VillageStoryProgress event's data.</summary>
    public class VillageStoryProgressData
    {
        public VillageStoryData Story { get; set; }
        public int StepDone { get; set; }
        public Guid FinderId { get; set; }
        public string FinderName { get; set; }
    }

    /// <summary>Mirrors RavenNest.Models.VillageStoryCompleted, the VillageStoryCompleted event's data.</summary>
    public class VillageStoryCompletedData
    {
        public VillageStoryData Story { get; set; }
        public string[] Unlocks { get; set; }
        public string DecorUnlocked { get; set; }
        public StreamQuestHelperData[] Helpers { get; set; }
    }
}
