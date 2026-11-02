using System;
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    /// <summary>
    /// Own boats on the server (api/boats): what the shipwright on Away builds, and !boat build. See docs/own-boats.md,
    /// phase 4.
    /// </summary>
    public class BoatsApi
    {
        private readonly IApiRequestBuilderProvider request;

        public BoatsApi(IApiRequestBuilderProvider request)
        {
            this.request = request;
        }

        public Task<BoatBlueprint[]> GetBlueprintsAsync()
        {
            return request.Create().Identifier("blueprints").Build()
                .SendAsync<BoatBlueprint[]>(ApiRequestTarget.Boats, ApiRequestType.Get);
        }

        public Task<BoatBuildResult> BuildAsync(Guid characterId, string boat)
        {
            return request.Create().Identifier(characterId.ToString()).Method("build").Build()
                .SendAsync<BoatBuildResult, BoatBuildRequest>(ApiRequestTarget.Boats, ApiRequestType.Post, new BoatBuildRequest { Boat = boat });
        }

        /// <summary>!boat look: the equipped boat's look and what the player owns of each part ("Customizing boats").</summary>
        public Task<BoatLookResult> GetLookAsync(Guid characterId)
        {
            return request.Create().Identifier(characterId.ToString()).Method("look").Build()
                .SendAsync<BoatLookResult>(ApiRequestTarget.Boats, ApiRequestType.Get);
        }

        /// <summary>!boat sails 7: one part of the equipped boat's look, to one the player owns.</summary>
        public Task<BoatLookResult> SetLookAsync(Guid characterId, string slot, int variant)
        {
            return request.Create().Identifier(characterId.ToString()).Method("look").AddParameter(slot).AddParameter(variant.ToString()).Build()
                .SendAsync<BoatLookResult>(ApiRequestTarget.Boats, ApiRequestType.Post);
        }

        /// <summary>A guest climbed aboard the owner's boat ("Guests aboard"): the owner gets a cut of their fish at sea.</summary>
        public Task<bool> GuestAboardAsync(Guid ownerId, Guid guestId)
        {
            return request.Create().Identifier(ownerId.ToString()).Method("guest-aboard").AddParameter(guestId.ToString()).Build()
                .SendAsync<bool>(ApiRequestTarget.Boats, ApiRequestType.Post);
        }

        /// <summary>A guest stepped off; a whole trip (they sailed a fair way) counts towards the owner's Ferryman titles.</summary>
        public Task<bool> GuestAshoreAsync(Guid ownerId, Guid guestId, bool trip)
        {
            return request.Create().Identifier(ownerId.ToString()).Method("guest-ashore").AddParameter(guestId.ToString()).AddParameter(trip ? "trip" : "none").Build()
                .SendAsync<bool>(ApiRequestTarget.Boats, ApiRequestType.Post);
        }
    }
}
