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

        public Task<BeginSessionResult> BeginSessionAsync(string clientVersion, string accessKey, float syncTime)
        {
            return request.Create()
                .AddParameter(clientVersion)
                .AddParameter(accessKey)
                .AddParameter(syncTime.ToString())
                .Build()
                .SendAsync<BeginSessionResult>(ApiRequestTarget.Game, ApiRequestType.Post);
        }

        public Task AttachPlayersAsync(Guid[] ids)
        {
            return request.Create()
                .Method("attach")
                .AddParameter("values", ids)
                .Build()
                .SendAsync(ApiRequestTarget.Game, ApiRequestType.Post);
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
    }
}