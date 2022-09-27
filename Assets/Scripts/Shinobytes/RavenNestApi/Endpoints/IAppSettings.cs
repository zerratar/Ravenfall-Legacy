namespace RavenNest.SDK.Endpoints
{
    public interface IAppSettings
    {
        string WebApiEndpoint { get; }
        string WebApiAuthEndpoint { get; }
        string WebSocketEndpoint { get; }
        string TcpApiEndpoint { get; }
    }

    //
}