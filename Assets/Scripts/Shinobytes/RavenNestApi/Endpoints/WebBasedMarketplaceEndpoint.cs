using RavenNest.Models;
using System;
using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    public class VillageApi
    {
        private readonly IApiRequestBuilderProvider request;
        private readonly RavenNestClient client;
        private readonly ILogger logger;
        public VillageApi(
            RavenNestClient client,
            ILogger logger,
            IApiRequestBuilderProvider request)
        {
            this.client = client;
            this.logger = logger;
            this.request = request;
        }

        public Task<bool> AssignPlayerAsync(int slot, Guid characterId)
        {
            return request.Create()
                    .Identifier(slot.ToString())
                    .Method("assign-character")
                    .AddParameter(characterId.ToString())
                    .Build()
                    .SendAsync<bool>(
                        ApiRequestTarget.Village,
                        ApiRequestType.Get);
        }
        public Task<bool> BuildHouseAsync(int slot, int type)
        {
            return request.Create()
                .Identifier(slot.ToString())
                .Method("build")
                .AddParameter(type.ToString())
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Village,
                    ApiRequestType.Get);
        }

        public Task<bool> AssignVillageAsync(int type, Guid[] characterIds)
        {
            return request.Create()
                .Method("assign-village")
                .Build()
                .SendAsync<bool>(ApiRequestTarget.Village, ApiRequestType.Post, new VillageAssignRequest
                {
                    Type = type,
                    CharacterIds = characterIds
                });
        }

        public Task<VillageInfo> GetAsync()
        {
            return request.Create()
               .Build()
               .SendAsync<VillageInfo>(
                   ApiRequestTarget.Village,
                   ApiRequestType.Get);
        }

        public Task<bool> RemoveHouseAsync(int slot)
        {
            return request.Create()
                .Identifier(slot.ToString())
                .Method("remove")
                .Build()
                .SendAsync<bool>(
                    ApiRequestTarget.Village,
                    ApiRequestType.Get);
        }
    }

    public class VillageAssignRequest
    {
        public int Type { get; set; }
        public Guid[] CharacterIds { get; set; }
    }
}