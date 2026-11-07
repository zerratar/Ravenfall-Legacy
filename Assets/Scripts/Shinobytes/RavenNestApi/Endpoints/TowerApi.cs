using System;
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    /// <summary>
    /// The tower on the server (api/tower, docs/tower-design.md): the stream's floor and quests, kills on the floor,
    /// boss attempts and clears. The server keeps the progress; the game reports what happens on the floor.
    /// </summary>
    public class TowerApi
    {
        private readonly IApiRequestBuilderProvider request;

        public TowerApi(IApiRequestBuilderProvider request)
        {
            this.request = request;
        }

        public Task<TowerState> GetAsync()
        {
            return request.Create().Build().SendAsync<TowerState>(ApiRequestTarget.Tower, ApiRequestType.Get);
        }

        /// <summary>Kills on the floor since the last report, in the quests' text form ("defeat-hall-goblin:3;...").</summary>
        public Task<TowerState> ProgressAsync(int floor, string counts)
        {
            return request.Create().Identifier(floor.ToString()).Method("progress").AddParameter(Uri.EscapeDataString(counts)).Build()
                .SendAsync<TowerState>(ApiRequestTarget.Tower, ApiRequestType.Post);
        }

        /// <summary>Kills per character and kind on the floors, for the characters' own quests (Mossgate's chains).</summary>
        public Task<bool> KillsAsync(TowerKillReport report)
        {
            return request.Create().Identifier("kills").Build()
                .SendAsync<bool, TowerKillReport>(ApiRequestTarget.Tower, ApiRequestType.Post, report);
        }

        public Task<TowerState> BossStartedAsync(int floor)
        {
            return request.Create().Identifier(floor.ToString()).Method("boss").Build()
                .SendAsync<TowerState>(ApiRequestTarget.Tower, ApiRequestType.Post);
        }

        public Task<TowerClearResult> ClearedAsync(int floor, Guid[] characterIds)
        {
            return request.Create().Identifier(floor.ToString()).Method("cleared").AddParameter("values", characterIds).Build()
                .SendAsync<TowerClearResult>(ApiRequestTarget.Tower, ApiRequestType.Post);
        }
    }
}
