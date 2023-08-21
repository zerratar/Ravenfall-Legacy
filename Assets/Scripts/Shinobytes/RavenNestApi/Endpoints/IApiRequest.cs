using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    public enum ApiRequestType
    {
        Get,
        Post,
        Update,
        Remove,
    }

    public enum ApiRequestTarget
    {
        Game,
        Items,
        Players,
        Auth,
        Marketplace,
        Village,
        Twitch,
        Clan,
    }
}