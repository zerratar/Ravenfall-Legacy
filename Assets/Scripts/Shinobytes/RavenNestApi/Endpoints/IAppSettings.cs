namespace RavenNest.SDK.Endpoints
{
    public interface IAppSettings
    {
        string ApiEndpoint { get; }
        string ApiAuthEndpoint { get; }
        string WebSocketEndpoint { get; }
    }

    //
}