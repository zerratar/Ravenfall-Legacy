using RavenNest.Models;
using System;
using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    public class ClanApi
    {
        private readonly IApiRequestBuilderProvider request;
        private readonly RavenNestClient client;
        private readonly ILogger logger;
        public ClanApi(
            RavenNestClient client,
            ILogger logger,
            IApiRequestBuilderProvider request)
        {
            this.client = client;
            this.logger = logger;
            this.request = request;
        }

        public Task<ClanInfo> GetClanInfoAsync(Guid auditCharacterId)
        {
            return request.Create()
                    .Method("info")
                    .AddParameter(auditCharacterId.ToString())
                    .Build().SendAsync<ClanInfo>(ApiRequestTarget.Clan, ApiRequestType.Get);
        }

        public Task<ClanStats> GetClanStatsAsync(Guid auditCharacterId)
        {
            return request.Create()
                    .Method("stats")
                    .AddParameter(auditCharacterId.ToString())
                    .Build().SendAsync<ClanStats>(ApiRequestTarget.Clan, ApiRequestType.Get);
        }

        internal Task<ChangeRoleResult> PromoteMemberAsync(Guid auditCharacterId, Guid characterId, string argument)
        {
            return request.Create()
                .Method("promote-member")
                .AddParameter(auditCharacterId.ToString())
                .AddParameter(characterId.ToString())
                .AddParameter(argument)
                .Build().SendAsync<ChangeRoleResult>(ApiRequestTarget.Clan, ApiRequestType.Get);
        }

        internal Task<ChangeRoleResult> DemoteMemberAsync(Guid auditCharacterId, Guid characterId, string argument)
        {
            return request.Create()
                .Method("demote-member")
                .AddParameter(auditCharacterId.ToString())
                .AddParameter(characterId.ToString())
                .AddParameter(argument)
                .Build().SendAsync<ChangeRoleResult>(ApiRequestTarget.Clan, ApiRequestType.Get);
        }

        internal Task<ClanInviteResult> InvitePlayerAsync(Guid auditCharacterId, Guid characterId)
        {
            return request.Create()
                .Method("invite")
                .AddParameter(auditCharacterId.ToString())
                .AddParameter(characterId.ToString())
                .Build().SendAsync<ClanInviteResult>(ApiRequestTarget.Clan, ApiRequestType.Get);
        }

        internal Task<ClanDeclineResult> DeclineInviteAsync(Guid auditCharacterId, string argument)
        {
            return request.Create()
                .Method("decline-invite")
                .AddParameter(auditCharacterId.ToString())
                .AddParameter(argument ?? "-")
                .Build().SendAsync<ClanDeclineResult>(ApiRequestTarget.Clan, ApiRequestType.Get);
        }

        internal Task<JoinClanResult> AcceptInviteAsync(Guid auditCharacterId, string argument)
        {
            return request.Create()
                .Method("accept-invite")
                .AddParameter(auditCharacterId.ToString())
                .AddParameter(argument ?? "-")
                .Build().SendAsync<JoinClanResult>(ApiRequestTarget.Clan, ApiRequestType.Get);
        }


        internal Task<ClanLeaveResult> LeaveAsync(Guid auditCharacterId)
        {
            return request.Create()
                .Method("leave")
                .AddParameter(auditCharacterId.ToString())
                .Build().SendAsync<ClanLeaveResult>(ApiRequestTarget.Clan, ApiRequestType.Get);
        }

        internal Task<JoinClanResult> JoinAsync(Guid clanOwnerUserId, Guid characterId)
        {
            return request.Create()
                .Identifier("v2")
                .Method("join")
                .AddParameter(clanOwnerUserId.ToString())
                .AddParameter(characterId.ToString())
                .Build().SendAsync<JoinClanResult>(ApiRequestTarget.Clan, ApiRequestType.Get);
        }

        internal Task<bool> RemoveMemberAsync(Guid auditCharacterId, Guid targetCharacterId)
        {
            return request.Create()
                .Method("remove")
                .AddParameter(auditCharacterId.ToString())
                .AddParameter(targetCharacterId.ToString())
                .Build().SendAsync<bool>(ApiRequestTarget.Clan, ApiRequestType.Get);
        }
    }
}