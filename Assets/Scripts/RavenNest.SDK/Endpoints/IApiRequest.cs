using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    public interface IApiRequest
    {
        Task<TResult> SendAsync<TResult, TModel>(ApiRequestTarget target, ApiRequestType type, TModel model);
        Task<TResult> SendAsync<TResult>(ApiRequestTarget target, ApiRequestType type);
        Task SendAsync(ApiRequestTarget target, ApiRequestType type);
    }

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
        Village
    }
}