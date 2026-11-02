using System;
using System.Threading.Tasks;
using RavenNest.Models;

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

        public Task<QuestList> GetAsync(Guid characterId)
        {
            return request.Create().Identifier(characterId.ToString()).Build()
                .SendAsync<QuestList>(ApiRequestTarget.Quests, ApiRequestType.Get);
        }

        /// <summary>Takes one of today's offers (slot 0 to 2), this week's quest (slot -1) or today's seasonal quest (-2).</summary>
        public Task<QuestActionResult> AcceptAsync(Guid characterId, int slot)
        {
            return request.Create().Identifier(characterId.ToString()).Method("accept").AddParameter(slot.ToString()).Build()
                .SendAsync<QuestActionResult>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        public Task<QuestActionResult> AbandonAsync(Guid characterId, Guid questId)
        {
            return request.Create().Identifier(characterId.ToString()).Method("abandon").AddParameter(questId).Build()
                .SendAsync<QuestActionResult>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>Begins a quest chain ("cat", "fish", "founder", "shipwright", "pumpkin").</summary>
        public Task<QuestActionResult> BeginChainAsync(Guid characterId, string key)
        {
            return request.Create().Identifier(characterId.ToString()).Method("chain").AddParameter(key).Build()
                .SendAsync<QuestActionResult>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>Hands in what the chain's delivery step asks for, from the character's inventory.</summary>
        public Task<QuestActionResult> DeliverAsync(Guid characterId)
        {
            return request.Create().Identifier(characterId.ToString()).Method("deliver").Build()
                .SendAsync<QuestActionResult>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>Answers the chain's riddle or choice (from 1).</summary>
        public Task<QuestActionResult> AnswerAsync(Guid characterId, int answer)
        {
            return request.Create().Identifier(characterId.ToString()).Method("answer").AddParameter(answer.ToString()).Build()
                .SendAsync<QuestActionResult>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>The character's achievements, the titles earned and the one worn.</summary>
        public Task<AchievementList> GetAchievementsAsync(Guid characterId)
        {
            return request.Create().Identifier(characterId.ToString()).Method("achievements").Build()
                .SendAsync<AchievementList>(ApiRequestTarget.Quests, ApiRequestType.Get);
        }

        /// <summary>Wears an earned title, or none with "none".</summary>
        public Task<TitleResult> SetTitleAsync(Guid characterId, string title)
        {
            return request.Create().Identifier(characterId.ToString()).Method("title").AddParameter(title).Build()
                .SendAsync<TitleResult>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>The stream's streamer quests and what the streamer can set aside (api/quests/streamer).</summary>
        public Task<StreamerQuestList> GetStreamerAsync()
        {
            return request.Create().Identifier("streamer").Build()
                .SendAsync<StreamerQuestList>(ApiRequestTarget.Quests, ApiRequestType.Get);
        }

        /// <summary>The running streamer quests with one character's progress on them (!quest).</summary>
        public Task<StreamerQuestInfo[]> GetStreamerForAsync(Guid characterId)
        {
            return request.Create().Identifier("streamer").Method(characterId.ToString()).Build()
                .SendAsync<StreamerQuestInfo[]>(ApiRequestTarget.Quests, ApiRequestType.Get);
        }

        /// <summary>Posts a streamer quest; the server sets its reward aside.</summary>
        public Task<StreamerQuestResult> PostStreamerAsync(StreamerQuestRequest quest)
        {
            return request.Create().Identifier("streamer").Method("post").Build()
                .SendAsync<StreamerQuestResult, StreamerQuestRequest>(ApiRequestTarget.Quests, ApiRequestType.Post, quest);
        }

        public Task<StreamerQuestResult> StopStreamerAsync(Guid questId)
        {
            return request.Create().Identifier("streamer").Method("stop").AddParameter(questId.ToString()).Build()
                .SendAsync<StreamerQuestResult>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>The streamer's story lines and what they have unlocked (api/quests/village).</summary>
        public Task<VillageLog> GetVillageAsync()
        {
            return request.Create().Identifier("village").Build()
                .SendAsync<VillageLog>(ApiRequestTarget.Quests, ApiRequestType.Get);
        }

        public Task<VillageStoryResult> BeginStoryAsync(string key) => StoryAction("begin", key);

        /// <summary>The streamer talks to the story's NPC: a talk step is done.</summary>
        public Task<VillageStoryResult> TalkStoryAsync(string key) => StoryAction("talk", key);

        public Task<VillageStoryResult> AbandonStoryAsync(string key) => StoryAction("abandon", key);

        /// <summary>The streamer makes the story's choice (from 1).</summary>
        public Task<VillageStoryResult> ChooseStoryAsync(string key, int option)
        {
            return request.Create().Identifier("village").Method("choose").AddParameter(key).AddParameter(option.ToString()).Build()
                .SendAsync<VillageStoryResult>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>A viewer brings the story's delivery item from their inventory (!quest give).</summary>
        public Task<VillageStoryResult> GiveStoryAsync(Guid characterId)
        {
            return request.Create().Identifier("village").Method("give").AddParameter(characterId.ToString()).Build()
                .SendAsync<VillageStoryResult>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>The streamer's character finished the Ravebridge island quests: the title "of Ravebridge".</summary>
        public Task<VillageStoryResult> RavebridgeDoneAsync()
        {
            return request.Create().Identifier("village").Method("ravebridge-done").Build()
                .SendAsync<VillageStoryResult>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>A hero mode wave run ended: the streamer's best, the titles of waves 10, 25 and 50, the best's rank.</summary>
        public Task<HeroRunResult> HeroRunAsync(int waves, int kills, int seconds)
        {
            return request.Create().Identifier("hero").Method("run").AddParameter(waves.ToString()).AddParameter(kills.ToString()).AddParameter(seconds.ToString()).Build()
                .SendAsync<HeroRunResult>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>Coins a viewer pays in hero mode (!hero elite); the coins left, or -1 when they had too few.</summary>
        public Task<long> HeroSpendAsync(Guid characterId, long coins)
        {
            return request.Create().Identifier("hero").Method("spend").AddParameter(characterId.ToString()).AddParameter(coins.ToString()).Build()
                .SendAsync<long>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>Every streamer's best hero mode run, the best first.</summary>
        public Task<HeroLeaderboard> HeroLeaderboardAsync()
        {
            return request.Create().Identifier("hero").Method("leaderboard").Build()
                .SendAsync<HeroLeaderboard>(ApiRequestTarget.Quests, ApiRequestType.Get);
        }

        /// <summary>A run of the day's challenge ended (day as yyyyMMdd, UTC; twists as "Hunters,Blessed-steel", for the
        /// website's board): the streamer's best of the day and its place.</summary>
        public Task<HeroRunResult> HeroDailyAsync(int day, int waves, int kills, int seconds, string twists)
        {
            return request.Create().Identifier("hero").Method("daily").AddParameter(day.ToString()).AddParameter(waves.ToString()).AddParameter(kills.ToString()).AddParameter(seconds.ToString()).AddParameter(twists).Build()
                .SendAsync<HeroRunResult>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>Today's challenge: every streamer's best run of it, the best first.</summary>
        public Task<HeroLeaderboard> HeroDailyLeaderboardAsync()
        {
            return request.Create().Identifier("hero").Method("daily").AddParameter("leaderboard").Build()
                .SendAsync<HeroLeaderboard>(ApiRequestTarget.Quests, ApiRequestType.Get);
        }

        private Task<VillageStoryResult> StoryAction(string action, string key)
        {
            return request.Create().Identifier("village").Method(action).AddParameter(key).Build()
                .SendAsync<VillageStoryResult>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        /// <summary>The titles worn by the characters in this stream, for the name tags.</summary>
        public Task<SessionTitles> GetTitlesAsync()
        {
            return request.Create().Identifier("titles").Build()
                .SendAsync<SessionTitles>(ApiRequestTarget.Quests, ApiRequestType.Get);
        }

        /// <summary>The stream's quest: the running one and today's three offers (api/quests/stream).</summary>
        public Task<StreamQuestState> GetStreamAsync()
        {
            return request.Create().Identifier("stream").Build()
                .SendAsync<StreamQuestState>(ApiRequestTarget.Quests, ApiRequestType.Get);
        }

        /// <summary>Starts one of today's stream quest offers (slot 0 to 2).</summary>
        public Task<StreamQuestResult> StartStreamAsync(int slot)
        {
            return request.Create().Identifier("stream").Method("start").AddParameter(slot.ToString()).Build()
                .SendAsync<StreamQuestResult>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }

        public Task<StreamQuestResult> StopStreamAsync()
        {
            return request.Create().Identifier("stream").Method("stop").Build()
                .SendAsync<StreamQuestResult>(ApiRequestTarget.Quests, ApiRequestType.Post);
        }
    }
}
