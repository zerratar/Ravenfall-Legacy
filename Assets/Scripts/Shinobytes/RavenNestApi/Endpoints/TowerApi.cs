using System;
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    /// <summary>
    /// The tower on the server (api/tower, docs/tower-design.md): the stream's part and its objectives, what happens
    /// on the part, boss attempts and clears. The server keeps the progress; the game reports what happens on the
    /// part. "floor" in the calls is the part's step: its number counted through every floor of the tower.
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

        /// <summary>The stream's best climbers, for the stone and the statue at the foot of the tower in the main world (WorldTower).</summary>
        public Task<WorldTowerClimber[]> ClimbersAsync()
        {
            return request.Create().Identifier("climbers").Build().SendAsync<WorldTowerClimber[]>(ApiRequestTarget.Tower, ApiRequestType.Get);
        }

        /// <summary>
        /// What happened on the part since the last report, in the objectives' text form: kills per kill objective, every
        /// kill and hidden things found ("defeat-hall-goblin:3;kills:5;find-boots:1").
        /// </summary>
        public Task<TowerState> ProgressAsync(int floor, string counts)
        {
            return request.Create().Identifier(floor.ToString()).Method("progress").AddParameter(Uri.EscapeDataString(counts)).Build()
                .SendAsync<TowerState>(ApiRequestTarget.Tower, ApiRequestType.Post);
        }

        /// <summary>Kills per character and kind in the tower, for the characters' own quests (the towns' chains).</summary>
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

        /// <summary>The part's guardian fell: who took part, whose blows landed last and how strong the group was.</summary>
        public Task<TowerClearResult> ClearedAsync(int floor, TowerClearReport report)
        {
            return request.Create().Identifier(floor.ToString()).Method("cleared").Build()
                .SendAsync<TowerClearResult, TowerClearReport>(ApiRequestTarget.Tower, ApiRequestType.Post, report);
        }
    }
}
