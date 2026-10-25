using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using RavenNest.Models;
namespace RavenNest.SDK.Endpoints
{
    public class GameApi
    {
        private readonly RavenNestClient client;
        private readonly ILogger logger;
        private readonly IApiRequestBuilderProvider request;

        public GameApi(RavenNestClient client, ILogger logger, IApiRequestBuilderProvider request)
        {
            this.client = client;
            this.logger = logger;
            this.request = request;
        }

        public Task<GameInfo> GetAsync()
        {
            return request.Create().Build().SendAsync<GameInfo>(ApiRequestTarget.Game, ApiRequestType.Get);
        }

        /// <summary>The signed token for the v2 connection; fails on a server without v2.</summary>
        public Task<string> GetConnectionTokenAsync()
        {
            return request.Create()
                .Method("connection-token")
                .Build()
                .SendAsync<string>(ApiRequestTarget.Game, ApiRequestType.Get);
        }

        public Task<SessionToken> BeginSessionAsync(
            string clientVersion,
            string accessKey,
            bool local,
            float syncTime)
        {
            return request.Create()
                .AddParameter(clientVersion)
                .AddParameter(accessKey)
                .AddParameter("value1", local)
                .AddParameter("value2", syncTime)
                .Build()
                .SendAsync<SessionToken>(ApiRequestTarget.Game, ApiRequestType.Post);
        }

        public Task<BeginSessionResult> BeginSessionAsync(string clientVersion, string accessKey, float syncTime, bool updateSkipped = false)
        {
            if (updateSkipped)
            {
                return request.Create()
                    .AddParameter(clientVersion)
                    .AddParameter(accessKey)
                    .AddParameter(syncTime.ToString())
                    .AddParameter("skip-update")
                    .Build()
                    .SendAsync<BeginSessionResult>(ApiRequestTarget.Game, ApiRequestType.Post);
            }

            return request.Create()
                .AddParameter(clientVersion)
                .AddParameter(accessKey)
                .AddParameter(syncTime.ToString())
                .Build()
                .SendAsync<BeginSessionResult>(ApiRequestTarget.Game, ApiRequestType.Post);
        }

        /// <summary>Who the server remembers in this stream, in the state file's format (see ServerPlayerRestore).</summary>
        public Task<Assets.Scripts.RestorableGameState> GetSessionPlayersAsync()
        {
            return request.Create()
                .Method("session-players")
                .Build()
                .SendAsync<Assets.Scripts.RestorableGameState>(ApiRequestTarget.Game, ApiRequestType.Get);
        }

        /// <summary>The players in the game now, replacing the server's list.</summary>
        public Task<bool> SetSessionPlayersAsync(Guid[] characterIds)
        {
            return request.Create()
                .Method("session-players")
                .Build()
                .SendAsync<bool, Guid[]>(ApiRequestTarget.Game, ApiRequestType.Post, characterIds);
        }

        /// <summary>Players the streamer chose not to restore: the server no longer counts them as in this stream.</summary>
        public Task<int> ReleasePlayersAsync(Guid[] characterIds)
        {
            return request.Create()
                .Method("release-players")
                .Build()
                .SendAsync<int, Guid[]>(ApiRequestTarget.Game, ApiRequestType.Post, characterIds);
        }

        public Task AttachPlayersAsync(Guid[] ids)
        {
            return request.Create()
                .Method("attach")
                .AddParameter("values", ids)
                .Build()
                .SendAsync(ApiRequestTarget.Game, ApiRequestType.Post);
        }

        internal Task<EventItemReward[]> GetRaidRewardsAsync(Guid[] ids)
        {
            return request.Create()
                .Method("raid-reward")
                .AddParameter("values", ids)
                .Build()
                .SendAsync<EventItemReward[]>(ApiRequestTarget.Game, ApiRequestType.Post);
        }

        /// <summary>Hero mode's reward chest after a wave run (the hero first, then the top helpers).</summary>
        internal Task<EventItemReward[]> GetHeroChestAsync(int waves, Guid[] ids)
        {
            return request.Create()
                .Identifier(waves + "")
                .Method("hero-chest")
                .AddParameter("values", ids)
                .Build()
                .SendAsync<EventItemReward[]>(ApiRequestTarget.Game, ApiRequestType.Post);
        }

        internal Task<EventItemReward[]> GetDungeonRewardsAsync(DungeonTier tier, Guid[] ids)
        {
            return request.Create()
                .Identifier((int)tier + "")
                .Method("dungeon-reward")
                .AddParameter("values", ids)
                .Build()
                .SendAsync<EventItemReward[]>(ApiRequestTarget.Game, ApiRequestType.Post);
        }

        public Task<bool> ClearPlayersAsync()
        {
            return request.Create()
                 .Method("clear-players")
                    .Build()
                    .SendAsync<bool>(ApiRequestTarget.Game, ApiRequestType.Get);
        }

        public Task<ScrollInfoCollection> GetScrollsAsync(PlayerController plr)
        {
            return request.Create()
                   .Method("get-scrolls")
                   .AddParameter(plr.Id.ToString())
                   .Build()
                   .SendAsync<ScrollInfoCollection>(ApiRequestTarget.Game, ApiRequestType.Get);
        }
        public Task<ScrollUseResult> ActivateRaidAsync(PlayerController plr)
        {
            return request.Create()
                   .Method("use-scroll")
                   .AddParameter(plr.Id.ToString())
                   .AddParameter(ScrollType.Raid.ToString())
                   .Build()
                   .SendAsync<ScrollUseResult>(ApiRequestTarget.Game, ApiRequestType.Get);
        }

        public Task<ScrollUseResult> ActivateDungeonAsync(PlayerController plr)
        {
            return request.Create()
                   .Method("use-scroll")
                   .AddParameter(plr.Id.ToString())
                   .AddParameter(ScrollType.Dungeon.ToString())
                   .Build()
                   .SendAsync<ScrollUseResult>(ApiRequestTarget.Game, ApiRequestType.Get);
        }

        public Task<ExpMultiplier> GetExpMultiplierAsync()
        {
            return request.Create()
                  .Method("exp-multiplier")
                  .Build()
                  .SendAsync<ExpMultiplier>(ApiRequestTarget.Game, ApiRequestType.Get);
        }

        public Task<ScrollUseResult> ActivateExpMultiplierAsync(PlayerController plr)
        {
            return request.Create()
                   .Method("use-scroll")
                   .AddParameter(plr.Id.ToString())
                   .AddParameter(ScrollType.Experience.ToString())
                   .Build()
                   .SendAsync<ScrollUseResult>(ApiRequestTarget.Game, ApiRequestType.Get);
        }

        public Task<int> ActivateExpMultiplierAsync(PlayerController plr, int usageCount)
        {
            return request.Create()
                   .Method("use-exp-scroll")
                   .AddParameter(plr.Id.ToString())
                   .AddParameter(usageCount.ToString())
                   .Build()
                   .SendAsync<int>(ApiRequestTarget.Game, ApiRequestType.Get);
        }

        public Task<UseExpScrollResult> UseExpScrollAsync(PlayerController plr, int usageCount)
        {
            return request.Create()
                   .Method("use-exp-scroll-new")
                   .AddParameter(plr.Id.ToString())
                   .AddParameter(usageCount.ToString())
                   .Build()
                   .SendAsync<UseExpScrollResult>(ApiRequestTarget.Game, ApiRequestType.Get);
        }

        public Task<bool> EndSessionAndRaidAsync(string username, bool war)
        {
            return request.Create()
                .Method("raid")
                .AddParameter(username)
                .AddParameter(war.ToString())
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Game, ApiRequestType.Get);
        }

        public Task EndSessionAsync()
        {
            return request.Create()
                .Method("end")
                .Build()
                .SendAsync(ApiRequestTarget.Game, ApiRequestType.Get);
        }

        public void EndSession()
        {
            request.Create()
                .Method("end")
                .Build()
                .Send(ApiRequestTarget.Game, ApiRequestType.Get);
        }

        //public Task EndSessionAsync()
        //{
        //    return request.Create()
        //        .Build()
        //        .SendAsync(ApiRequestTarget.Game, ApiRequestType.Post);
        //}

        public Task<EventCollection> PollEventsAsync(int revision)
        {
            return request.Create()
                .Method("events")
                .AddParameter(revision.ToString())
                .Build()
                .SendAsync<EventCollection>(ApiRequestTarget.Game, ApiRequestType.Get);
        }

        public async Task<bool> ClearLogoAsync(string userId)
        {
            try
            {
                return await request.Create()
                    .Method("clear-logo")
                    .AddParameter(userId)
                    .Build()
                    .SendAsync<bool>(ApiRequestTarget.Twitch, ApiRequestType.Get);
            }
            catch
            {
                return false;
            }
        }

        internal async Task<bool> ReportExceptionAsync(string arguments, Exception exc, [CallerMemberName] string caller = null)
        {
            try
            {
                return await request.Create()
                    .Method("report-exception")
                    .AddParameter("error", caller + "(" + arguments + "): " + exc)
                    .Build()
                    .SendAsync<bool>(ApiRequestTarget.Game, ApiRequestType.Post);
            }
            catch
            {
                return false;
            }
        }

        internal async Task<object> UploadLogFileAsync(Guid requestId, byte[] content)
        {
            try
            {
                return await request.Create()
                    .Method("upload-log")
                    .AddParameter(requestId.ToString())
                    .Build()
                    .SendAsync<object, byte[]>(ApiRequestTarget.Game, ApiRequestType.Post, content);
            }
            catch (Exception ex)
            {
                Shinobytes.Debug.LogError($"Failed to upload log file: {ex}");
                return null;
            }
        }

        internal async Task<object> UploadStateDataAsync(Guid requestId, byte[] content)
        {
            try
            {
                return await request.Create()
                    .Method("upload-state")
                    .AddParameter(requestId.ToString())
                    .Build()
                    .SendAsync<object, byte[]>(ApiRequestTarget.Game, ApiRequestType.Post, content);
            }
            catch (Exception ex)
            {
                Shinobytes.Debug.LogError($"Failed to upload game state: {ex}");
                return null;
            }
        }
    }
}