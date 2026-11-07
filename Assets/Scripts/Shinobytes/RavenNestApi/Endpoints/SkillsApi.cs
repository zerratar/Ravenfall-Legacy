using System;
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    /// <summary>
    /// The skill trees on the server (api/skills, combat v2, docs/combat-skills-design.md): the build, learning a node,
    /// the loadout and respecs. The server saves; the game follows the build it answers with.
    /// </summary>
    public class SkillsApi
    {
        private readonly IApiRequestBuilderProvider request;

        public SkillsApi(IApiRequestBuilderProvider request)
        {
            this.request = request;
        }

        public Task<SkillBuildInfo> GetAsync(Guid characterId)
        {
            return request.Create().Identifier(characterId.ToString()).Build()
                .SendAsync<SkillBuildInfo>(ApiRequestTarget.Skills, ApiRequestType.Get);
        }

        public Task<SkillBuildResult> LearnAsync(Guid characterId, string node)
        {
            return request.Create().Identifier(characterId.ToString()).Method("learn").AddParameter(Uri.EscapeDataString(node)).Build()
                .SendAsync<SkillBuildResult>(ApiRequestTarget.Skills, ApiRequestType.Post);
        }

        /// <summary>Puts an active in a slot (1 based); a null or empty node empties the slot.</summary>
        public Task<SkillBuildResult> LoadoutAsync(Guid characterId, int slot, string node)
        {
            return request.Create().Identifier(characterId.ToString()).Method("loadout").AddParameter(slot.ToString()).AddParameter(string.IsNullOrWhiteSpace(node) ? "-" : Uri.EscapeDataString(node)).Build()
                .SendAsync<SkillBuildResult>(ApiRequestTarget.Skills, ApiRequestType.Post);
        }

        public Task<SkillBuildResult> RespecAsync(Guid characterId)
        {
            return request.Create().Identifier(characterId.ToString()).Method("respec").Build()
                .SendAsync<SkillBuildResult>(ApiRequestTarget.Skills, ApiRequestType.Post);
        }
    }
}
